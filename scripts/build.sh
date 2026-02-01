#!/bin/bash
# ANXAgentSwarm - Production Build Script
# 
# This script builds the complete ANXAgentSwarm application for production deployment.
#
# Usage:
#   ./scripts/build.sh              # Build all components
#   ./scripts/build.sh api          # Build only API
#   ./scripts/build.sh web          # Build only frontend
#   ./scripts/build.sh docker       # Build Docker images
#

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"
BUILD_OUTPUT="$PROJECT_ROOT/build"
API_PROJECT="$PROJECT_ROOT/src/ANXAgentSwarm.Api"
WEB_PROJECT="$PROJECT_ROOT/src/ANXAgentSwarm.Web"

# Functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

check_dependencies() {
    log_info "Checking dependencies..."
    
    if ! command -v dotnet &> /dev/null; then
        log_error "dotnet CLI is not installed"
        exit 1
    fi
    
    if ! command -v node &> /dev/null; then
        log_error "Node.js is not installed"
        exit 1
    fi
    
    if ! command -v npm &> /dev/null; then
        log_error "npm is not installed"
        exit 1
    fi
    
    log_success "All dependencies found"
}

clean_build() {
    log_info "Cleaning previous build..."
    rm -rf "$BUILD_OUTPUT"
    mkdir -p "$BUILD_OUTPUT"
    log_success "Build directory cleaned"
}

build_api() {
    log_info "Building API..."
    
    cd "$API_PROJECT"
    
    # Restore dependencies
    dotnet restore
    
    # Build in Release mode
    dotnet build -c Release --no-restore
    
    # Publish to build output
    dotnet publish -c Release -o "$BUILD_OUTPUT/api" --no-restore
    
    log_success "API built successfully"
}

build_web() {
    log_info "Building frontend..."
    
    cd "$WEB_PROJECT"
    
    # Install dependencies
    npm ci
    
    # Run type checking
    npm run build
    
    # Move build output
    mv dist "$BUILD_OUTPUT/web"
    
    log_success "Frontend built successfully"
}

build_docker() {
    log_info "Building Docker images..."
    
    cd "$PROJECT_ROOT"
    
    # Check if docker is available
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed"
        exit 1
    fi
    
    # Build API image
    log_info "Building API Docker image..."
    docker build -f Dockerfile.api -t anxagentswarm-api:latest .
    
    # Build Web image
    log_info "Building Web Docker image..."
    docker build -f Dockerfile.web -t anxagentswarm-web:latest .
    
    log_success "Docker images built successfully"
}

run_tests() {
    log_info "Running tests..."
    
    cd "$PROJECT_ROOT"
    
    # Run .NET tests
    dotnet test --configuration Release --no-build
    
    # Run frontend tests (if available)
    cd "$WEB_PROJECT"
    if [ -f "package.json" ] && grep -q '"test"' package.json; then
        npm test
    fi
    
    log_success "All tests passed"
}

print_summary() {
    echo ""
    echo "=========================================="
    echo "           Build Summary"
    echo "=========================================="
    echo ""
    echo "Build output: $BUILD_OUTPUT"
    echo ""
    
    if [ -d "$BUILD_OUTPUT/api" ]; then
        echo "✓ API: $BUILD_OUTPUT/api"
        echo "  Entry: dotnet ANXAgentSwarm.Api.dll"
    fi
    
    if [ -d "$BUILD_OUTPUT/web" ]; then
        echo "✓ Web: $BUILD_OUTPUT/web"
        echo "  Serve with any static file server"
    fi
    
    echo ""
    echo "=========================================="
}

# Main
main() {
    echo ""
    echo "=========================================="
    echo "  ANXAgentSwarm Production Build"
    echo "=========================================="
    echo ""
    
    local component="${1:-all}"
    
    check_dependencies
    
    case "$component" in
        api)
            clean_build
            build_api
            ;;
        web)
            clean_build
            build_web
            ;;
        docker)
            build_docker
            ;;
        test)
            run_tests
            ;;
        all)
            clean_build
            build_api
            build_web
            ;;
        *)
            log_error "Unknown component: $component"
            echo "Usage: $0 [api|web|docker|test|all]"
            exit 1
            ;;
    esac
    
    print_summary
}

main "$@"
