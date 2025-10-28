#!/bin/bash

# API-Minimizer Docker Build Agent
# Este script automatiza la compilación de las imágenes Docker del proyecto

set -e  # Exit on any error

# Colores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuración
PROJECT_NAME="api-minimizer"
REGISTRY=${DOCKER_REGISTRY:-""}
VERSION=${VERSION:-"latest"}
BUILD_TYPE=${BUILD_TYPE:-"all"}  # all, front, back

# Funciones de logging
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

# Función para mostrar ayuda
show_help() {
    echo "API-Minimizer Docker Build Agent"
    echo ""
    echo "Uso: $0 [OPCIONES]"
    echo ""
    echo "Opciones:"
    echo "  -t, --type TYPE     Tipo de build (all, front, back) [default: all]"
    echo "  -v, --version VER   Versión de la imagen [default: latest]"
    echo "  -r, --registry REG  Registro Docker [default: sin registro]"
    echo "  -c, --clean         Limpiar imágenes anteriores"
    echo "  -p, --push          Hacer push de las imágenes al registro"
    echo "  -h, --help          Mostrar esta ayuda"
    echo ""
    echo "Variables de entorno:"
    echo "  DOCKER_REGISTRY     Registro Docker"
    echo "  VERSION            Versión de la imagen"
    echo "  BUILD_TYPE         Tipo de build"
    echo ""
    echo "Ejemplos:"
    echo "  $0                    # Build completo"
    echo "  $0 -t front          # Solo build del frontend"
    echo "  $0 -v 1.0.0 -p       # Build con versión y push"
    echo "  $0 -c -t back        # Limpiar y build solo backend"
}

# Función para limpiar imágenes
clean_images() {
    log_info "Limpiando imágenes anteriores..."
    
    # Buscar y eliminar imágenes relacionadas con el proyecto
    local images=$(docker images | grep -E "(apiminimizer|apiminimizerback)" | awk '{print $3}')
    
    if [ -n "$images" ]; then
        echo "$images" | xargs -r docker rmi -f
        log_success "Imágenes limpiadas"
    else
        log_info "No se encontraron imágenes para limpiar"
    fi
}

# Función para construir imagen frontend
build_frontend() {
    log_info "Construyendo imagen frontend..."
    
    local image_name="${REGISTRY}apiminimizer"
    local tag="${image_name}:${VERSION}"
    
    docker build \
        -f ApiFront/Dockerfile \
        -t "$tag" \
        --build-arg BUILD_TYPE=Release \
        .
    
    if [ $? -eq 0 ]; then
        log_success "Imagen frontend construida: $tag"
        echo "$tag" >> .built_images
    else
        log_error "Error construyendo imagen frontend"
        exit 1
    fi
}

# Función para construir imagen backend
build_backend() {
    log_info "Construyendo imagen backend..."
    
    local image_name="${REGISTRY}apiminimizerback"
    local tag="${image_name}:${VERSION}"
    
    docker build \
        -f ApiBack/API-Minimizer-back/Dockerfile \
        -t "$tag" \
        --build-arg BUILD_TYPE=Release \
        .
    
    if [ $? -eq 0 ]; then
        log_success "Imagen backend construida: $tag"
        echo "$tag" >> .built_images
    else
        log_error "Error construyendo imagen backend"
        exit 1
    fi
}

# Función para hacer push de imágenes
push_images() {
    if [ -z "$REGISTRY" ]; then
        log_warning "No se especificó registro Docker. Saltando push."
        return
    fi
    
    log_info "Haciendo push de imágenes..."
    
    if [ -f .built_images ]; then
        while IFS= read -r image; do
            log_info "Push de: $image"
            docker push "$image"
            if [ $? -eq 0 ]; then
                log_success "Push exitoso: $image"
            else
                log_error "Error en push: $image"
                exit 1
            fi
        done < .built_images
    fi
}

# Función para verificar dependencias
check_dependencies() {
    log_info "Verificando dependencias..."
    
    if ! command -v docker &> /dev/null; then
        log_error "Docker no está instalado o no está en el PATH"
        exit 1
    fi
    
    if ! docker info &> /dev/null; then
        log_error "Docker no está ejecutándose"
        exit 1
    fi
    
    log_success "Dependencias verificadas"
}

# Función para mostrar resumen
show_summary() {
    log_info "Resumen del build:"
    echo "  Tipo de build: $BUILD_TYPE"
    echo "  Versión: $VERSION"
    echo "  Registro: ${REGISTRY:-"local"}"
    
    if [ -f .built_images ]; then
        echo "  Imágenes construidas:"
        while IFS= read -r image; do
            echo "    - $image"
        done < .built_images
    fi
}

# Procesar argumentos de línea de comandos
PUSH_IMAGES=false
CLEAN_IMAGES=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -t|--type)
            BUILD_TYPE="$2"
            shift 2
            ;;
        -v|--version)
            VERSION="$2"
            shift 2
            ;;
        -r|--registry)
            REGISTRY="$2"
            shift 2
            ;;
        -c|--clean)
            CLEAN_IMAGES=true
            shift
            ;;
        -p|--push)
            PUSH_IMAGES=true
            shift
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            log_error "Opción desconocida: $1"
            show_help
            exit 1
            ;;
    esac
done

# Inicializar archivo de imágenes construidas
> .built_images

# Función principal
main() {
    log_info "Iniciando API-Minimizer Docker Build Agent"
    log_info "Directorio de trabajo: $(pwd)"
    
    # Verificar dependencias
    check_dependencies
    
    # Limpiar imágenes si se solicita
    if [ "$CLEAN_IMAGES" = true ]; then
        clean_images
    fi
    
    # Construir imágenes según el tipo especificado
    case $BUILD_TYPE in
        "all")
            build_frontend
            build_backend
            ;;
        "front")
            build_frontend
            ;;
        "back")
            build_backend
            ;;
        *)
            log_error "Tipo de build inválido: $BUILD_TYPE"
            log_info "Tipos válidos: all, front, back"
            exit 1
            ;;
    esac
    
    # Hacer push si se solicita
    if [ "$PUSH_IMAGES" = true ]; then
        push_images
    fi
    
    # Mostrar resumen
    show_summary
    
    log_success "Build completado exitosamente"
}

# Ejecutar función principal
main "$@" 