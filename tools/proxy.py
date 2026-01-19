#!/usr/bin/env python3
"""
Local unauthenticated proxy that forwards to the authenticated upstream proxy.
This allows tools that don't support authenticated proxies to work through
the environment's proxy setup.
"""

import os
import socket
import threading
import select
import urllib.parse
import base64

# Configuration
LOCAL_HOST = '127.0.0.1'
LOCAL_PORT = 8888

# Get upstream proxy from environment
UPSTREAM_PROXY = os.environ.get('HTTP_PROXY') or os.environ.get('http_proxy')

if not UPSTREAM_PROXY:
    print("ERROR: No HTTP_PROXY environment variable found")
    exit(1)

# Parse upstream proxy URL
parsed = urllib.parse.urlparse(UPSTREAM_PROXY)
UPSTREAM_HOST = parsed.hostname
UPSTREAM_PORT = parsed.port or 80

# Build auth header if credentials present
PROXY_AUTH = None
if parsed.username:
    password = parsed.password or ''
    credentials = f"{urllib.parse.unquote(parsed.username)}:{urllib.parse.unquote(password)}"
    encoded = base64.b64encode(credentials.encode()).decode()
    PROXY_AUTH = f"Proxy-Authorization: Basic {encoded}"


def handle_client(client_socket):
    """Handle a client connection by forwarding through upstream proxy."""
    try:
        # Receive the request from client
        request = b''
        client_socket.settimeout(30)
        while True:
            chunk = client_socket.recv(4096)
            if not chunk:
                client_socket.close()
                return
            request += chunk
            if b'\r\n\r\n' in request:
                break

        if not request:
            client_socket.close()
            return

        # Check if this is a CONNECT request (HTTPS)
        request_str = request.decode('utf-8', errors='ignore')
        lines = request_str.split('\r\n')
        request_line = lines[0]
        parts = request_line.split()

        if len(parts) < 2:
            client_socket.close()
            return

        method = parts[0]

        # Connect to upstream proxy
        upstream_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        upstream_socket.settimeout(30)
        upstream_socket.connect((UPSTREAM_HOST, UPSTREAM_PORT))

        if method == 'CONNECT':
            # For HTTPS CONNECT requests, add proxy auth to the CONNECT request
            # Rebuild the request with auth header
            new_lines = [request_line]
            has_host = False
            for line in lines[1:]:
                if line == '':
                    break
                if line.lower().startswith('host:'):
                    has_host = True
                new_lines.append(line)

            if PROXY_AUTH:
                new_lines.append(PROXY_AUTH)
            new_lines.append('')
            new_lines.append('')

            modified_request = '\r\n'.join(new_lines).encode()
            upstream_socket.sendall(modified_request)

            # Read response from upstream
            response = b''
            while True:
                try:
                    chunk = upstream_socket.recv(4096)
                    if not chunk:
                        break
                    response += chunk
                    if b'\r\n\r\n' in response:
                        break
                except socket.timeout:
                    break

            # Send response to client
            client_socket.sendall(response)

            # Check if connection established
            response_line = response.split(b'\r\n')[0]
            if b'200' in response_line:
                # Tunnel data bidirectionally
                upstream_socket.settimeout(None)
                client_socket.settimeout(None)
                tunnel_data(client_socket, upstream_socket)
            else:
                print(f"CONNECT failed: {response_line}")
        else:
            # For HTTP requests, add proxy auth header
            new_lines = [request_line]
            for line in lines[1:]:
                if line == '':
                    break
                new_lines.append(line)

            if PROXY_AUTH:
                new_lines.append(PROXY_AUTH)

            # Add the body if present
            header_end = request.find(b'\r\n\r\n')
            body = request[header_end+4:] if header_end >= 0 else b''

            new_lines.append('')
            new_lines.append('')
            modified_request = '\r\n'.join(new_lines).encode() + body

            upstream_socket.sendall(modified_request)

            # Forward response back to client
            while True:
                try:
                    data = upstream_socket.recv(8192)
                    if not data:
                        break
                    client_socket.sendall(data)
                except:
                    break

        upstream_socket.close()
        client_socket.close()

    except Exception as e:
        print(f"Error handling client: {e}")
        import traceback
        traceback.print_exc()
        try:
            client_socket.close()
        except:
            pass


def tunnel_data(client_socket, upstream_socket):
    """Tunnel data bidirectionally between client and upstream."""
    sockets = [client_socket, upstream_socket]
    try:
        while True:
            readable, _, exceptional = select.select(sockets, [], sockets, 60)

            if exceptional:
                break

            if not readable:
                # Timeout
                break

            for sock in readable:
                try:
                    data = sock.recv(8192)
                    if not data:
                        return

                    if sock is client_socket:
                        upstream_socket.sendall(data)
                    else:
                        client_socket.sendall(data)
                except Exception as e:
                    return
    except Exception as e:
        pass


def main():
    """Start the proxy server."""
    server = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    server.bind((LOCAL_HOST, LOCAL_PORT))
    server.listen(50)

    print(f"Local proxy listening on {LOCAL_HOST}:{LOCAL_PORT}")
    print(f"Forwarding to upstream proxy at {UPSTREAM_HOST}:{UPSTREAM_PORT}")
    print(f"Proxy authentication: {'enabled' if PROXY_AUTH else 'disabled'}")

    while True:
        client_socket, addr = server.accept()
        client_thread = threading.Thread(target=handle_client, args=(client_socket,))
        client_thread.daemon = True
        client_thread.start()


if __name__ == '__main__':
    main()
