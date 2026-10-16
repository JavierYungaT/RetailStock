# syntax=docker/dockerfile:1
# =====================================================================================
# retailstock-front - imagen multi-stage
#   Etapa "build":   Node compila React/TypeScript con Vite -> archivos estáticos en /app/dist
#   Etapa "runtime": nginx sirve esos archivos (sin Node en producción) y hace de proxy hacia la API.
#
# Construir:  docker build -t retailstock-front ./retailstock-front
# =====================================================================================

FROM node:24-alpine AS build
WORKDIR /app

# Dependencias primero (capa cacheada mientras no cambie package-lock.json).
# "npm ci" instala EXACTAMENTE lo del lock (reproducible), a diferencia de "npm install".
COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# Vite "hornea" las variables VITE_* en el JavaScript AL COMPILAR (no se leen en tiempo de ejecución).
# "/api" = misma origin: nginx reenvía /api a la API, así el navegador no necesita CORS.
ARG VITE_API_URL=/api
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# -------------------------------------------------------------------------------------
# nginx-unprivileged: corre como usuario NO root y escucha en 8080 (no necesita el puerto 80)
FROM nginxinc/nginx-unprivileged:1.28-alpine AS runtime

COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 8080
