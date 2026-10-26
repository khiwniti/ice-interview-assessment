#!/bin/bash
set -euo pipefail

# =============================================================================
# Business Analytics - Installation Script
# =============================================================================
# Installs all prerequisites and sets up the development environment
# Supports: Ubuntu/Debian, RHEL/CentOS/Fedora, Alpine, macOS
# =============================================================================

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log() { echo -e "${BLUE}[$(date '+%H:%M:%S')]${NC} $*"; }
log_success() { echo -e "${GREEN}✓${NC} $*"; }
log_warn() { echo -e "${YELLOW}⚠${NC} $*"; }
log_error() { echo -e "${RED}✗${NC} $*" >&2; }

# Detect OS
detect_os() {
    if [[ -f /etc/os-release ]]; then
        . /etc/os-release
        OS=$ID
        VERSION=$VERSION_ID
    elif [[ "$OSTYPE" == "darwin"* ]]; then
        OS="macos"
    else
        OS="unknown"
    fi
    log "Detected OS: $OS $VERSION"
}

# Install Docker
install_docker() {
    if command -v docker &>/dev/null; then
        log_success "Docker already installed: $(docker --version)"
        return 0
    fi

    log "Installing Docker..."
    case $OS in
        ubuntu|debian)
            apt-get update
            apt-get install -y ca-certificates curl gnupg lsb-release
            install -m 0755 -d /etc/apt/keyrings
            curl -fsSL https://download.docker.com/linux/$OS/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
            echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/$OS $(lsb_release -cs) stable" | tee /etc/apt/sources.list.d/docker.list >/dev/null
            apt-get update
            apt-get install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
            ;;
        rhel|centos|fedora|rocky|almalinux)
            dnf -y install dnf-plugins-core
            dnf config-manager --add-repo https://download.docker.com/linux/centos/docker-ce.repo
            dnf install -y docker-ce docker-ce-cli containerd.io docker-compose-plugin
            systemctl enable --now docker
            ;;
        alpine)
            apk add --no-cache docker docker-compose
            rc-update add docker boot
            service docker start
            ;;
        macos)
            log_warn "On macOS, please install Docker Desktop from https://docker.com/products/docker-desktop"
            return 1
            ;;
        *)
            log_error "Unsupported OS for automatic Docker install: $OS"
            return 1
            ;;
    esac

    # Add user to docker group (Linux only)
    if [[ "$OS" != "macos" ]]; then
        usermod -aG docker "$SUDO_USER" 2>/dev/null || usermod -aG docker "$USER" 2>/dev/null || true
        log_warn "You may need to log out and back in for docker group changes to take effect"
    fi

    log_success "Docker installed"
}

# Install Docker Compose v2 (standalone)
install_docker_compose() {
    if docker compose version &>/dev/null; then
        log_success "Docker Compose v2 already available"
        return 0
    fi

    log "Installing Docker Compose v2..."
    COMPOSE_VERSION=$(curl -s https://api.github.com/repos/docker/compose/releases/latest | grep '"tag_name"' | cut -d'"' -f4)
    curl -SL "https://github.com/docker/compose/releases/download/${COMPOSE_VERSION}/docker-compose-$(uname -s)-$(uname -m)" -o /usr/local/bin/docker-compose
    chmod +x /usr/local/bin/docker-compose
    log_success "Docker Compose ${COMPOSE_VERSION} installed"
}

# Install .NET 8 SDK
install_dotnet() {
    if command -v dotnet &>/dev/null && dotnet --version | grep -q '^8\.'; then
        log_success ".NET 8 SDK already installed: $(dotnet --version)"
        return 0
    fi

    log "Installing .NET 8 SDK..."
    case $OS in
        ubuntu|debian)
            apt-get update
            apt-get install -y dotnet-sdk-8.0
            ;;
        rhel|centos|fedora|rocky|almalinux)
            dnf install -y dotnet-sdk-8.0
            ;;
        alpine)
            # Use Microsoft install script for Alpine
            wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
            chmod +x /tmp/dotnet-install.sh
            /tmp/dotnet-install.sh --channel 8.0 --version latest --install-dir /usr/share/dotnet
            ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
            ;;
        macos)
            if command -v brew &>/dev/null; then
                brew install --cask dotnet-sdk
            else
                log_warn "Install Homebrew first, then run: brew install --cask dotnet-sdk"
                return 1
            fi
            ;;
        *)
            log_warn "Automatic .NET install not supported on $OS. Use: https://dotnet.microsoft.com/download/dotnet/8.0"
            return 1
            ;;
    esac

    log_success ".NET 8 SDK installed: $(dotnet --version)"
}

# Install Git
install_git() {
    if command -v git &>/dev/null; then
        log_success "Git already installed: $(git --version)"
        return 0
    fi

    log "Installing Git..."
    case $OS in
        ubuntu|debian) apt-get install -y git ;;
        rhel|centos|fedora|rocky|almalinux) dnf install -y git ;;
        alpine) apk add --no-cache git ;;
        macos)
            if command -v brew &>/dev/null; then brew install git; else xcode-select --install; fi
            ;;
    esac
    log_success "Git installed"
}

# Install jq (JSON processor)
install_jq() {
    if command -v jq &>/dev/null; then
        log_success "jq already installed"
        return 0
    fi

    log "Installing jq..."
    case $OS in
        ubuntu|debian) apt-get install -y jq ;;
        rhel|centos|fedora|rocky|almalinux) dnf install -y jq ;;
        alpine) apk add --no-cache jq ;;
        macos)
            if command -v brew &>/dev/null; then brew install jq; else log_warn "Install jq manually"; fi
            ;;
    esac
    log_success "jq installed"
}

# Install shellcheck (for script validation)
install_shellcheck() {
    if command -v shellcheck &>/dev/null; then
        log_success "shellcheck already installed"
        return 0
    fi

    log "Installing shellcheck..."
    case $OS in
        ubuntu|debian) apt-get install -y shellcheck ;;
        rhel|centos|fedora|rocky|almalinux) dnf install -y shellcheck ;;
        alpine) apk add --no-cache shellcheck ;;
        macos)
            if command -v brew &>/dev/null; then brew install shellcheck; else log_warn "Install shellcheck manually"; fi
            ;;
    esac
    log_success "shellcheck installed"
}

# Main installation
main() {
    echo -e "${BLUE}===============================================================================${NC}"
    echo -e "${BLUE}  Business Analytics - Prerequisites Installation${NC}"
    echo -e "${BLUE}===============================================================================${NC}"
    echo ""

    detect_os

    # Check for root/sudo
    if [[ $EUID -ne 0 && "$OS" != "macos" ]]; then
        log_error "This script requires root privileges for package installation"
        log "Run with: sudo $0"
        exit 1
    fi

    # Install prerequisites
    install_docker
    install_docker_compose
    install_dotnet
    install_git
    install_jq
    install_shellcheck

    # Start Docker daemon (Linux only)
    if [[ "$OS" != "macos" ]]; then
        log "Starting Docker service..."
        systemctl enable --now docker 2>/dev/null || service docker start 2>/dev/null || rc-service docker start 2>/dev/null || true
        sleep 3
    fi

    # Verify installations
    echo ""
    log "Verifying installations..."
    docker --version && log_success "Docker: OK"
    docker compose version && log_success "Docker Compose: OK"
    dotnet --version 2>/dev/null && log_success ".NET SDK: OK" || log_warn ".NET SDK not in PATH (may need shell restart)"
    git --version && log_success "Git: OK"
    jq --version && log_success "jq: OK"
    shellcheck --version && log_success "shellcheck: OK"

    echo ""
    log_success "Installation complete!"
    echo ""
    echo "Next steps:"
    echo "  1. If on Linux, log out and back in (for docker group)"
    echo "  2. Clone the repository:"
    echo "       git clone https://github.com/khiwniti/ice-interview-assessment.git"
    echo "  3. Run the E2E test:"
    echo "       cd ice-interview-assessment && ./scripts/e2e-test.sh"
}

main "$@"