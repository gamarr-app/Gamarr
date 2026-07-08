# Dev loop for Gamarr. Every target exits non-zero on real failure and prints
# a single OK/FAILED line at the end — safe to run from agents/CI without
# piping through grep/tail (which masks exit codes).

DOTNET_ROOT := $(HOME)/.dotnet
DOTNET := DOTNET_ROOT=$(DOTNET_ROOT) PATH=$(DOTNET_ROOT):$(PATH) dotnet
LOG_DIR := /tmp/gamarr-make
SHELL := /bin/bash

.PHONY: all build backend frontend check test smoke smoke-stop seed help

all: build

help:
	@echo "make backend      - build src/Gamarr.sln"
	@echo "make frontend     - yarn build (production UI into _output/UI)"
	@echo "make build        - backend + frontend"
	@echo "make check        - frontend lint + typecheck + unit tests"
	@echo "make test         - backend unit tests (run unsandboxed; see CLAUDE.md)"
	@echo "make smoke        - start a throwaway instance on :6968 (repeat to restart)"
	@echo "make smoke-stop   - stop it and delete its data dir"
	@echo "make seed         - seed the smoke instance (games + fake indexer + a file)"

$(LOG_DIR):
	@mkdir -p $(LOG_DIR)

backend: | $(LOG_DIR)
	@$(DOTNET) build src/Gamarr.sln > $(LOG_DIR)/backend.log 2>&1 \
		&& echo "backend: OK ($$(ls -la _output/net10.0/Gamarr.Core.dll | awk '{print $$6, $$7, $$8}'))" \
		|| { grep -E " error " $(LOG_DIR)/backend.log | head -20; echo "backend: FAILED (full log: $(LOG_DIR)/backend.log)"; exit 1; }

frontend: | $(LOG_DIR)
	@yarn --silent build > $(LOG_DIR)/frontend.log 2>&1 \
		&& echo "frontend: OK" \
		|| { tail -30 $(LOG_DIR)/frontend.log; echo "frontend: FAILED (full log: $(LOG_DIR)/frontend.log)"; exit 1; }

build: backend frontend

check: | $(LOG_DIR)
	@yarn --silent lint > $(LOG_DIR)/lint.log 2>&1 \
		&& echo "lint: OK" \
		|| { tail -30 $(LOG_DIR)/lint.log; echo "lint: FAILED"; exit 1; }
	@yarn --silent tsc --noEmit -p frontend > $(LOG_DIR)/tsc.log 2>&1 \
		&& echo "tsc: OK" \
		|| { tail -30 $(LOG_DIR)/tsc.log; echo "tsc: FAILED"; exit 1; }
	@yarn --silent test > $(LOG_DIR)/jest.log 2>&1 \
		&& { grep -E "^Tests:" $(LOG_DIR)/jest.log; echo "jest: OK"; } \
		|| { tail -30 $(LOG_DIR)/jest.log; echo "jest: FAILED"; exit 1; }

# NOTE for agents: the Claude Code sandbox SIGKILLs dotnet testhost children.
# Run `make test` with the sandbox disabled.
test: | $(LOG_DIR)
	@$(DOTNET) test src/NzbDrone.Core.Test/Gamarr.Core.Test.csproj \
		--filter "Category!=AutomationTest&Category!=IntegrationTest" \
		> $(LOG_DIR)/test.log 2>&1 \
		&& { grep -E "Passed!|Failed!" $(LOG_DIR)/test.log; echo "test: OK"; } \
		|| { grep -E "Failed |Passed!|Failed!|error" $(LOG_DIR)/test.log | head -30; echo "test: FAILED (full log: $(LOG_DIR)/test.log)"; exit 1; }

smoke:
	@scripts/dev/smoke.sh start

smoke-stop:
	@scripts/dev/smoke.sh stop

seed:
	@scripts/dev/seed.sh
