FROM node:24-slim AS build
WORKDIR /src
RUN npm install --global pnpm@12.6.0
COPY ["Chunkora.Web/package.json", "Chunkora.Web/pnpm-lock.yaml", "./"]
RUN pnpm install --frozen-lockfile
COPY Chunkora.Web/ .
RUN pnpm run build

FROM caddy:2-alpine AS final
COPY --from=build /src/dist/ /srv/
COPY Chunkora.Web/deploy/Caddyfile /etc/caddy/Caddyfile