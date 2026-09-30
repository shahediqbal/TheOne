FROM node:24-bookworm-slim AS build
WORKDIR /source/apps/website
COPY apps/website/package*.json ./
RUN npm ci
COPY apps/website/ ./
COPY packages/ /source/packages/
RUN npm run build
FROM node:24-bookworm-slim
ENV NODE_ENV=production PORT=3000 HOSTNAME=0.0.0.0
WORKDIR /app
COPY --from=build --chown=node:node /source/apps/website/.next/standalone/ ./
COPY --from=build --chown=node:node /source/apps/website/.next/static/ ./apps/website/.next/static/
COPY --from=build --chown=node:node /source/apps/website/public/ ./apps/website/public/
USER node
EXPOSE 3000
CMD ["node", "apps/website/server.js"]
