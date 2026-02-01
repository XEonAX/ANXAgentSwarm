#!/bin/bash
# ANXAgentSwarm - Docker Deployment Script
#
# Usage:
#   ./scripts/deploy-docker.sh          # Build and start all services
#   ./scripts/deploy-docker.sh start    # Start existing containers
#   ./scripts/deploy-docker.sh stop     # Stop all containers
#   ./scripts/deploy-docker.sh logs     # View logs
#   ./scripts/deploy-docker.sh clean    # Remove all containers and volumes
#

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(dirname "$SCRIPT_DIR")"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warning() { echo -e "${YELLOW}[WARNING]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

check_docker() {
    if ! command -v docker &> /dev/null; then
        log_error "Docker is not installed"
        exit 1
    fi
    
    if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
        log_error "Docker Compose is not installed"
        exit 1
    fi
}

compose_cmd() {
    if docker compose version &> /dev/null 2>&1; then
        docker compose "$@"
    else
        docker-compose "$@"
    fi
}

build() {
    log_info "Building Docker images..."
    cd "$PROJECT_ROOT"
    compose_cmd build --no-cache
    log_success "Docker images built"
}

start() {
    log_info "Starting services..."
    cd "$PROJECT_ROOT"
    compose_cmd up -d
    
    log_info "Waiting for services to be healthy..."
    sleep 10
    
    # Check health
    compose_cmd ps
    
    echo ""
    log_success "Services started!"
    echo ""
    echo "Access the application at:"
    echo "  Web UI:   http://localhost"
    echo "  API:      http://localhost:5046"
    echo "  Ollama:   http://localhost:11434"
    echo ""
}

stop() {
    log_info "Stopping services..."
    cd "$PROJECT_ROOT"
    compose_cmd down
    log_success "Services stopped"
}

logs() {
    cd "$PROJECT_ROOT"
    compose_cmd logs -f "$@"
}

status() {
    cd "$PROJECT_ROOT"
    compose_cmd ps
}

clean() {
    log_warning "This will remove all containers, images, and volumes!"
    read -p "Are you sure? (y/N) " -n 1 -r
    echo
    
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        log_info "Cleaning up..."
        cd "$PROJECT_ROOT"
        compose_cmd down -v --rmi all
        log_success "Cleanup complete"
    else
        log_info "Cancelled"
    fi
}

pull_model() {
    local model="${1:-gemma3}"
    log_info "Pulling Ollama model: $model"
    docker exec anxagentswarm-ollama ollama pull "$model"
    log_success "Model $model pulled"
}

health_check() {
    log_info "Checking service health..."
    
    # Check API
    if curl -sf http://localhost:5046/health > /dev/null; then
        echo -e "  API:    ${GREEN}✓ Healthy${NC}"
    else
        echo -e "  API:    ${RED}✗ Unhealthy${NC}"
    fi
    
    # Check Web
    if curl -sf http://localhost/ > /dev/null; then
        echo -e "  Web:    ${GREEN}✓ Healthy${NC}"
    else
        echo -e "  Web:    ${RED}✗ Unhealthy${NC}"
    fi
    
    # Check Ollama
    if curl -sf http://localhost:11434/api/tags > /dev/null; then
        echo -e "  Ollama: ${GREEN}✓ Healthy${NC}"
    else
        echo -e "  Ollama: ${RED}✗ Unhealthy${NC}"
    fi
}

usage() {
    echo "Usage: $0 [command]"
    echo ""
    echo "Commands:"
    echo "  build      Build Docker images"
    echo "  start      Start all services"
    echo "  stop       Stop all services"
    echo "  restart    Restart all services"
    echo "  logs       View service logs"
    echo "  status     Show service status"
    echo "  health     Check service health"
    echo "  pull-model Pull an Ollama model"
    echo "  clean      Remove all containers and volumes"
    echo "  help       Show this help"
    echo ""
}

main() {
    check_docker
    
    local command="${1:-help}"
    shift || true
    
    case "$command" in
        build)
            build
            ;;
        start)
            start
            ;;
        stop)
            stop
            ;;
        restart)
            stop
            start
            ;;
        logs)
            logs "$@"
            ;;
        status)
            status
            ;;
        health)
            health_check
            ;;
        pull-model)
            pull_model "$@"
            ;;
        clean)
            clean
            ;;
        help|--help|-h)
            usage
            ;;
        *)
            log_error "Unknown command: $command"
            usage
            exit 1
            ;;
    esac
}

main "$@"
