# API-Minimizer Docker Build Agent

Este agente automatiza la compilación, testing y despliegue de las imágenes Docker del proyecto API-Minimizer.

## 🚀 Características

- **Build automatizado** de imágenes frontend y backend
- **Testing integrado** con health checks
- **CI/CD pipeline** con GitHub Actions
- **Múltiples ambientes** (desarrollo, staging, producción)
- **Cache optimizado** para builds más rápidos
- **Logging colorizado** para mejor experiencia
- **Gestión de versiones** flexible

## 📁 Estructura del Agente

```
API-Minimizer/
├── build-agent.sh              # Script principal del agente
├── Makefile                    # Comandos simplificados
├── docker-compose.build.yml    # Configuración para builds
├── .dockerignore              # Archivos ignorados en build
├── .github/workflows/         # CI/CD pipeline
│   └── docker-build.yml
└── DOCKER_BUILD_AGENT.md      # Esta documentación
```

## 🛠️ Instalación

1. **Clonar el repositorio:**
   ```bash
   git clone <tu-repositorio>
   cd API-Minimizer
   ```

2. **Dar permisos de ejecución:**
   ```bash
   chmod +x build-agent.sh
   ```

3. **Verificar dependencias:**
   ```bash
   docker --version
   docker-compose --version
   ```

## 🎯 Uso Básico

### Usando el script principal

```bash
# Build completo (frontend + backend)
./build-agent.sh

# Build solo frontend
./build-agent.sh -t front

# Build solo backend
./build-agent.sh -t back

# Build con versión específica
./build-agent.sh -v 1.0.0

# Build con registro Docker
./build-agent.sh -r myregistry.com/

# Build con limpieza previa
./build-agent.sh -c

# Build y push al registro
./build-agent.sh -p
```

### Usando Makefile (recomendado)

```bash
# Ver todos los comandos disponibles
make help

# Build completo
make build

# Build solo frontend
make build-front

# Build solo backend
make build-back

# Test de las imágenes
make test

# Deploy en staging
make deploy-staging

# Deploy en producción
make deploy-production
```

## ⚙️ Configuración

### Variables de Entorno

```bash
# Registro Docker (opcional)
export DOCKER_REGISTRY="myregistry.com/"

# Versión de las imágenes
export VERSION="1.0.0"

# Tipo de build
export BUILD_TYPE="all"  # all, front, back
```

### Archivo de Configuración

Puedes crear un archivo `.env` en la raíz del proyecto:

```bash
# .env
DOCKER_REGISTRY=myregistry.com/
VERSION=1.0.0
BUILD_TYPE=all
```

## 🔧 Comandos Avanzados

### Build con Cache Optimizado

```bash
# Usando docker-compose con cache
make build-with-cache

# O directamente
docker-compose -f docker-compose.build.yml --profile build build
```

### Testing de Imágenes

```bash
# Test completo
make test

# Test solo frontend
make test-front

# Test solo backend
make test-back
```

### Gestión de Contenedores

```bash
# Ver estado de contenedores
make status

# Ver logs
make logs

# Ver logs de staging
make logs-staging

# Detener todos los contenedores
make stop
```

### Limpieza y Mantenimiento

```bash
# Limpiar imágenes del proyecto
make clean

# Limpiar recursos Docker no utilizados
make docker-prune

# Ver estadísticas de Docker
make docker-stats

# Listar imágenes del proyecto
make images
```

## 🚀 CI/CD Pipeline

El agente incluye un pipeline de GitHub Actions que se ejecuta automáticamente:

### Triggers
- Push a `main` o `develop`
- Creación de tags `v*`
- Pull requests a `main` o `develop`

### Jobs
1. **Build & Test**: Construye y testea las imágenes
2. **Security Scan**: Escaneo de seguridad con Trivy
3. **Deploy Staging**: Despliegue automático a staging
4. **Deploy Production**: Despliegue a producción (solo tags)

### Configuración del Pipeline

Para usar el pipeline, necesitas:

1. **Configurar secrets en GitHub:**
   - `DOCKER_REGISTRY`: URL del registro Docker
   - `DOCKER_USERNAME`: Usuario del registro
   - `DOCKER_PASSWORD`: Contraseña del registro

2. **Configurar environments:**
   - `staging`: Para despliegues de staging
   - `production`: Para despliegues de producción

## 📊 Monitoreo y Logs

### Health Checks

Las imágenes incluyen health checks automáticos:

```bash
# Verificar health de un contenedor
docker inspect --format='{{.State.Health.Status}}' <container-name>

# Ver logs de health checks
docker inspect --format='{{range .State.Health.Log}}{{.Output}}{{end}}' <container-name>
```

### Logs Estructurados

```bash
# Logs en tiempo real
make logs

# Logs con filtros
docker-compose logs -f --tail=100 api-minimizer

# Logs de errores
docker-compose logs --tail=50 | grep ERROR
```

## 🔒 Seguridad

### Escaneo de Vulnerabilidades

El pipeline incluye escaneo automático con Trivy:

```bash
# Escaneo manual
docker run --rm -v /var/run/docker.sock:/var/run/docker.sock \
  aquasec/trivy image apiminimizer:latest
```

### Buenas Prácticas

- ✅ Usar imágenes base oficiales
- ✅ Ejecutar como usuario no-root
- ✅ Minimizar capas de imagen
- ✅ No incluir secrets en imágenes
- ✅ Usar multi-stage builds

## 🐛 Troubleshooting

### Problemas Comunes

1. **Error de permisos:**
   ```bash
   chmod +x build-agent.sh
   ```

2. **Docker no está ejecutándose:**
   ```bash
   sudo systemctl start docker
   ```

3. **Error de memoria:**
   ```bash
   # Aumentar memoria de Docker
   docker system prune -a
   ```

4. **Error de red:**
   ```bash
   # Reiniciar red de Docker
   docker network prune
   ```

### Logs de Debug

```bash
# Build con debug
./build-agent.sh --debug

# Docker build verbose
docker build --progress=plain -f ApiFront/Dockerfile .
```

## 📈 Optimización

### Build Cache

```bash
# Usar cache de capas
docker build --cache-from apiminimizer:latest .

# Cache con BuildKit
DOCKER_BUILDKIT=1 docker build .
```

### Multi-Platform Build

```bash
# Build para múltiples arquitecturas
docker buildx build --platform linux/amd64,linux/arm64 .
```

## 🤝 Contribución

Para contribuir al agente:

1. Fork el repositorio
2. Crea una rama para tu feature
3. Haz commit de tus cambios
4. Push a la rama
5. Crea un Pull Request

## 📝 Licencia

Este agente está bajo la misma licencia que el proyecto principal.

## 🆘 Soporte

Si tienes problemas:

1. Revisa la sección de troubleshooting
2. Verifica los logs con `make logs`
3. Abre un issue en el repositorio
4. Contacta al equipo de desarrollo

---

**¡Happy Docker Building! 🐳** 