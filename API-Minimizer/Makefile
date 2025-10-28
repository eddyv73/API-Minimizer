# API-Minimizer Docker Build Agent Makefile
# Simplifica los comandos para construir y gestionar las imágenes Docker

.PHONY: help build build-front build-back clean test push deploy-staging deploy-production

# Variables
VERSION ?= latest
REGISTRY ?= 
BUILD_TYPE ?= all
DOCKER_COMPOSE_BUILD = docker-compose -f docker-compose.build.yml

# Colores para output
RED := \033[0;31m
GREEN := \033[0;32m
YELLOW := \033[1;33m
BLUE := \033[0;34m
NC := \033[0m # No Color

help: ## Mostrar esta ayuda
	@echo "$(BLUE)API-Minimizer Docker Build Agent$(NC)"
	@echo ""
	@echo "$(YELLOW)Comandos disponibles:$(NC)"
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "  $(GREEN)%-20s$(NC) %s\n", $$1, $$2}' $(MAKEFILE_LIST)
	@echo ""
	@echo "$(YELLOW)Variables:$(NC)"
	@echo "  VERSION=$(VERSION)"
	@echo "  REGISTRY=$(REGISTRY)"
	@echo "  BUILD_TYPE=$(BUILD_TYPE)"
	@echo ""
	@echo "$(YELLOW)Ejemplos:$(NC)"
	@echo "  make build VERSION=1.0.0"
	@echo "  make build-front REGISTRY=myregistry.com/"
	@echo "  make test BUILD_TYPE=front"

build: ## Construir todas las imágenes
	@echo "$(BLUE)Construyendo todas las imágenes...$(NC)"
	@chmod +x build-agent.sh
	@./build-agent.sh -t $(BUILD_TYPE) -v $(VERSION) -r $(REGISTRY)

build-front: ## Construir solo la imagen del frontend
	@echo "$(BLUE)Construyendo imagen frontend...$(NC)"
	@chmod +x build-agent.sh
	@./build-agent.sh -t front -v $(VERSION) -r $(REGISTRY)

build-back: ## Construir solo la imagen del backend
	@echo "$(BLUE)Construyendo imagen backend...$(NC)"
	@chmod +x build-agent.sh
	@./build-agent.sh -t back -v $(VERSION) -r $(REGISTRY)

build-with-cache: ## Construir con cache optimizado usando docker-compose
	@echo "$(BLUE)Construyendo con cache optimizado...$(NC)"
	@VERSION=$(VERSION) REGISTRY=$(REGISTRY) $(DOCKER_COMPOSE_BUILD) --profile build build

clean: ## Limpiar imágenes anteriores
	@echo "$(BLUE)Limpiando imágenes anteriores...$(NC)"
	@chmod +x build-agent.sh
	@./build-agent.sh -c

test: ## Ejecutar tests de las imágenes construidas
	@echo "$(BLUE)Ejecutando tests de las imágenes...$(NC)"
	@VERSION=$(VERSION) REGISTRY=$(REGISTRY) $(DOCKER_COMPOSE_BUILD) --profile test up -d
	@sleep 15
	@echo "$(GREEN)Tests iniciados. Verificando health checks...$(NC)"
	@docker ps --filter "name=api-minimizer" --format "table {{.Names}}\t{{.Status}}"
	@VERSION=$(VERSION) REGISTRY=$(REGISTRY) $(DOCKER_COMPOSE_BUILD) --profile test down

test-front: ## Testear solo el frontend
	@echo "$(BLUE)Testeando frontend...$(NC)"
	@VERSION=$(VERSION) REGISTRY=$(REGISTRY) $(DOCKER_COMPOSE_BUILD) --profile test up -d api-minimizer-front-test
	@sleep 10
	@docker ps --filter "name=api-minimizer-front" --format "table {{.Names}}\t{{.Status}}"
	@VERSION=$(VERSION) REGISTRY=$(REGISTRY) $(DOCKER_COMPOSE_BUILD) --profile test down

test-back: ## Testear solo el backend
	@echo "$(BLUE)Testeando backend...$(NC)"
	@VERSION=$(VERSION) REGISTRY=$(REGISTRY) $(DOCKER_COMPOSE_BUILD) --profile test up -d api-minimizer-back-test
	@sleep 10
	@docker ps --filter "name=api-minimizer-back" --format "table {{.Names}}\t{{.Status}}"
	@VERSION=$(VERSION) REGISTRY=$(REGISTRY) $(DOCKER_COMPOSE_BUILD) --profile test down

push: ## Hacer push de las imágenes al registro
	@echo "$(BLUE)Haciendo push de las imágenes...$(NC)"
	@chmod +x build-agent.sh
	@./build-agent.sh -p -v $(VERSION) -r $(REGISTRY)

deploy-staging: ## Desplegar en ambiente de staging
	@echo "$(BLUE)Desplegando en staging...$(NC)"
	@VERSION=$(VERSION) REGISTRY=$(REGISTRY) $(DOCKER_COMPOSE_BUILD) --profile staging up -d
	@echo "$(GREEN)Staging desplegado en puertos 8082 (frontend) y 8083 (backend)$(NC)"

deploy-production: ## Desplegar en producción (requiere confirmación)
	@echo "$(RED)¿Estás seguro de que quieres desplegar en producción? (y/N)$(NC)"
	@read -p "" confirm && [ "$$confirm" = "y" ] || exit 1
	@echo "$(BLUE)Desplegando en producción...$(NC)"
	@VERSION=$(VERSION) REGISTRY=$(REGISTRY) docker-compose up -d
	@echo "$(GREEN)Producción desplegada$(NC)"

logs: ## Mostrar logs de los contenedores
	@docker-compose logs -f

logs-staging: ## Mostrar logs del ambiente de staging
	@VERSION=$(VERSION) REGISTRY=$(REGISTRY) $(DOCKER_COMPOSE_BUILD) --profile staging logs -f

stop: ## Detener todos los contenedores
	@echo "$(BLUE)Deteniendo contenedores...$(NC)"
	@docker-compose down
	@VERSION=$(VERSION) REGISTRY=$(REGISTRY) $(DOCKER_COMPOSE_BUILD) --profile staging down
	@VERSION=$(VERSION) REGISTRY=$(REGISTRY) $(DOCKER_COMPOSE_BUILD) --profile test down

status: ## Mostrar estado de los contenedores
	@echo "$(BLUE)Estado de los contenedores:$(NC)"
	@docker ps --filter "name=api-minimizer" --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"

images: ## Listar imágenes del proyecto
	@echo "$(BLUE)Imágenes del proyecto:$(NC)"
	@docker images | grep -E "(apiminimizer|api-minimizer)" || echo "No se encontraron imágenes"

# Comandos de desarrollo
dev-build: ## Build rápido para desarrollo
	@echo "$(BLUE)Build rápido para desarrollo...$(NC)"
	@docker-compose build

dev-up: ## Levantar ambiente de desarrollo
	@echo "$(BLUE)Levantando ambiente de desarrollo...$(NC)"
	@docker-compose up -d

dev-down: ## Bajar ambiente de desarrollo
	@echo "$(BLUE)Bajando ambiente de desarrollo...$(NC)"
	@docker-compose down

# Comandos de utilidad
docker-prune: ## Limpiar recursos Docker no utilizados
	@echo "$(BLUE)Limpiando recursos Docker...$(NC)"
	@docker system prune -f
	@docker volume prune -f
	@docker network prune -f

docker-stats: ## Mostrar estadísticas de Docker
	@echo "$(BLUE)Estadísticas de Docker:$(NC)"
	@docker stats --no-stream

# Comando por defecto
.DEFAULT_GOAL := help 