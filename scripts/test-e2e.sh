#!/bin/bash
# ANXAgentSwarm - E2E Test Runner Script
#
# Usage:
#   ./scripts/test-e2e.sh              # Run all E2E tests
#   ./scripts/test-e2e.sh --ui         # Run with Playwright UI
#   ./scripts/test-e2e.sh --headed     # Run in headed mode
#   ./scripts/test-e2e.sh --project=chromium  # Run specific project
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
WEB_PROJECT="$PROJECT_ROOT/src/ANXAgentSwarm.Web"

# Colors
BLUE='\033[0;34m'
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

check_dependencies() {
    if ! command -v node &> /dev/null; then
        log_error "Node.js is not installed"
        exit 1
    fi
    
    cd "$WEB_PROJECT"
    if [ ! -d "node_modules" ]; then
        log_info "Installing dependencies..."
        npm ci
    fi
    
    if [ ! -d "node_modules/@playwright" ]; then
        log_info "Installing Playwright browsers..."
        npx playwright install --with-deps
    fi
}

run_tests() {
    log_info "Running E2E tests..."
    cd "$WEB_PROJECT"
    
    # Pass all arguments to Playwright
    npx playwright test "$@"
    
    log_success "E2E tests completed"
}

main() {
    echo ""
    echo "=========================================="
    echo "  ANXAgentSwarm E2E Test Runner"
    echo "=========================================="
    echo ""
    
    check_dependencies
    run_tests "$@"
}

main "$@"
