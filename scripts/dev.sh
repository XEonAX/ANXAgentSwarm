#!/bin/bash
# ANXAgentSwarm - Development Server Script
#
# Usage:
#   ./scripts/dev.sh          # Start both API and Web dev servers
#   ./scripts/dev.sh api      # Start only API
#   ./scripts/dev.sh web      # Start only frontend
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
API_PROJECT="$PROJECT_ROOT/src/ANXAgentSwarm.Api"
WEB_PROJECT="$PROJECT_ROOT/src/ANXAgentSwarm.Web"

# Colors
BLUE='\033[0;34m'
GREEN='\033[0;32m'
NC='\033[0m'

log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

start_api() {
    log_info "Starting API development server..."
    cd "$API_PROJECT"
    dotnet watch run &
    API_PID=$!
    echo $API_PID > /tmp/anxagentswarm-api.pid
}

start_web() {
    log_info "Starting Web development server..."
    cd "$WEB_PROJECT"
    npm run dev &
    WEB_PID=$!
    echo $WEB_PID > /tmp/anxagentswarm-web.pid
}

cleanup() {
    log_info "Stopping development servers..."
    
    if [ -f /tmp/anxagentswarm-api.pid ]; then
        kill $(cat /tmp/anxagentswarm-api.pid) 2>/dev/null || true
        rm /tmp/anxagentswarm-api.pid
    fi
    
    if [ -f /tmp/anxagentswarm-web.pid ]; then
        kill $(cat /tmp/anxagentswarm-web.pid) 2>/dev/null || true
        rm /tmp/anxagentswarm-web.pid
    fi
    
    exit 0
}

trap cleanup SIGINT SIGTERM

main() {
    local component="${1:-all}"
    
    echo ""
    echo "=========================================="
    echo "  ANXAgentSwarm Development Server"
    echo "=========================================="
    echo ""
    
    case "$component" in
        api)
            start_api
            wait $API_PID
            ;;
        web)
            start_web
            wait $WEB_PID
            ;;
        all)
            start_api
            sleep 5  # Wait for API to start
            start_web
            echo ""
            echo -e "${GREEN}Development servers started:${NC}"
            echo "  API: http://localhost:5046"
            echo "  Web: http://localhost:5173"
            echo ""
            echo "Press Ctrl+C to stop all servers"
            echo ""
            wait
            ;;
        *)
            echo "Usage: $0 [api|web|all]"
            exit 1
            ;;
    esac
}

main "$@"
