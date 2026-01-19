#!/usr/bin/env python3
"""
Script to rename Movie -> Game throughout the codebase.
This handles:
1. Content replacement in files
2. File renaming
3. Directory renaming
"""

import os
import re
import shutil

ROOT_DIR = "/home/user/Gamarr"

# Directories to skip
SKIP_DIRS = {'.git', 'node_modules', '_output', '_tests', 'obj', 'bin', '.nuget', '_artifacts', '_ReSharper.Caches'}

# File extensions to process for content replacement
TEXT_EXTENSIONS = {
    '.cs', '.csproj', '.sln', '.json', '.xml', '.config', '.props', '.targets',
    '.ts', '.tsx', '.js', '.jsx', '.css', '.scss', '.html', '.md', '.txt',
    '.yml', '.yaml', '.sh', '.ps1', '.bat', '.cmd'
}

# Patterns to replace (order matters for some)
REPLACEMENTS = [
    # Plural forms first
    ('MovieMetadata', 'GameMetadata'),
    ('MovieFiles', 'GameFiles'),
    ('MovieFile', 'GameFile'),
    ('MovieStatistics', 'GameStatistics'),
    ('MovieStats', 'GameStats'),
    ('MovieCredits', 'GameCredits'),
    ('MovieTranslation', 'GameTranslation'),
    ('MovieTranslations', 'GameTranslations'),
    ('MovieCollection', 'GameCollection'),
    ('MovieCollections', 'GameCollections'),
    ('MovieHistory', 'GameHistory'),
    ('MovieQueue', 'GameQueue'),
    ('MovieResource', 'GameResource'),
    ('MovieRepository', 'GameRepository'),
    ('MovieService', 'GameService'),
    ('MovieValidator', 'GameValidator'),
    ('MovieController', 'GameController'),
    ('MovieModule', 'GameModule'),
    ('MovieLookup', 'GameLookup'),
    ('MovieSearch', 'GameSearch'),
    ('ImportListMovies', 'ImportListGames'),
    ('ImportListMovie', 'ImportListGame'),
    ('DownloadedMovies', 'DownloadedGames'),
    ('DownloadedMovie', 'DownloadedGame'),

    # TMDB -> IGDB (for game metadata)
    ('TmdbId', 'IgdbId'),
    ('TMDB', 'IGDB'),
    ('Tmdb', 'Igdb'),
    ('tmdb', 'igdb'),

    # IMDB stays but rename some movie-specific patterns
    ('ImdbId', 'ImdbId'),  # Keep as-is for compatibility, games can have IMDB IDs too

    # Movie -> Game (general)
    ('Movies', 'Games'),
    ('Movie', 'Game'),
    ('movies', 'games'),
    ('movie', 'game'),
    ('MOVIE', 'GAME'),

    # Movie status types (InCinemas -> Announced, etc.)
    ('InCinemas', 'InDevelopment'),
    ('PhysicalRelease', 'PhysicalRelease'),  # Keep for games
    ('DigitalRelease', 'DigitalRelease'),    # Keep for games

    # Radarr -> Gamarr
    ('Radarr', 'Gamarr'),
    ('radarr', 'gamarr'),
    ('RADARR', 'GAMARR'),
]

def should_skip_dir(path):
    """Check if directory should be skipped."""
    for skip in SKIP_DIRS:
        if skip in path.split(os.sep):
            return True
    return False

def should_process_file(filepath):
    """Check if file should be processed for content replacement."""
    _, ext = os.path.splitext(filepath)
    return ext.lower() in TEXT_EXTENSIONS

def replace_content(content):
    """Replace movie terms with game terms in content."""
    for old, new in REPLACEMENTS:
        content = content.replace(old, new)
    return content

def process_file(filepath):
    """Process a single file for content replacement."""
    if not should_process_file(filepath):
        return False

    try:
        with open(filepath, 'r', encoding='utf-8', errors='ignore') as f:
            content = f.read()

        new_content = replace_content(content)

        if content != new_content:
            with open(filepath, 'w', encoding='utf-8') as f:
                f.write(new_content)
            return True
    except Exception as e:
        print(f"Error processing {filepath}: {e}")

    return False

def get_new_name(name):
    """Get the new name after replacements."""
    new_name = name
    for old, new in REPLACEMENTS:
        new_name = new_name.replace(old, new)
    return new_name

def rename_files_in_dir(dirpath):
    """Rename files in a directory."""
    renamed = []
    for name in os.listdir(dirpath):
        old_path = os.path.join(dirpath, name)
        if os.path.isfile(old_path):
            new_name = get_new_name(name)
            if new_name != name:
                new_path = os.path.join(dirpath, new_name)
                os.rename(old_path, new_path)
                renamed.append((name, new_name))
    return renamed

def rename_directories(root):
    """Rename directories from bottom up."""
    dirs_to_rename = []

    for dirpath, dirnames, filenames in os.walk(root, topdown=False):
        if should_skip_dir(dirpath):
            continue

        for dirname in dirnames:
            if dirname in SKIP_DIRS:
                continue

            new_name = get_new_name(dirname)
            if new_name != dirname:
                old_path = os.path.join(dirpath, dirname)
                new_path = os.path.join(dirpath, new_name)
                dirs_to_rename.append((old_path, new_path))

    for old_path, new_path in dirs_to_rename:
        if os.path.exists(old_path) and not os.path.exists(new_path):
            os.rename(old_path, new_path)
            print(f"Renamed dir: {old_path} -> {new_path}")

def main():
    print("Starting Movie -> Game rename...")

    # Phase 1: Replace content in files
    print("\n=== Phase 1: Content replacement ===")
    files_modified = 0
    for dirpath, dirnames, filenames in os.walk(ROOT_DIR):
        # Skip certain directories
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]

        if should_skip_dir(dirpath):
            continue

        for filename in filenames:
            filepath = os.path.join(dirpath, filename)
            if process_file(filepath):
                print(f"Modified: {filepath}")
                files_modified += 1

    print(f"\nModified {files_modified} files")

    # Phase 2: Rename files
    print("\n=== Phase 2: File renaming ===")
    for dirpath, dirnames, filenames in os.walk(ROOT_DIR, topdown=False):
        dirnames[:] = [d for d in dirnames if d not in SKIP_DIRS]

        if should_skip_dir(dirpath):
            continue

        for filename in filenames:
            old_path = os.path.join(dirpath, filename)
            new_name = get_new_name(filename)
            if new_name != filename:
                new_path = os.path.join(dirpath, new_name)
                if os.path.exists(old_path) and not os.path.exists(new_path):
                    os.rename(old_path, new_path)
                    print(f"Renamed file: {filename} -> {new_name}")

    # Phase 3: Rename directories
    print("\n=== Phase 3: Directory renaming ===")
    rename_directories(ROOT_DIR)

    print("\n=== Done ===")

if __name__ == '__main__':
    main()
