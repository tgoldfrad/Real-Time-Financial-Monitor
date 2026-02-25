# Build stage
FROM node:20-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# Runtime stage — tiny Nginx image (~25 MB)
FROM nginx:stable-alpine AS runtime

COPY --from=build /app/dist /usr/share/nginx/html

# Copy custom nginx config for SPA routing + backend proxy
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80

HEALTHCHECK --interval=15s --timeout=3s --start-period=5s --retries=3 \
    CMD wget -qO- http://localhost:80/ || exit 1
