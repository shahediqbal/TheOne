FROM node:24-bookworm-slim AS build
WORKDIR /source/apps/management
COPY apps/management/package*.json ./
RUN npm ci
COPY apps/management/ ./
COPY packages/ /source/packages/
ARG VITE_WEBSITE_PUBLIC_ORIGIN
RUN test -n "$VITE_WEBSITE_PUBLIC_ORIGIN"
ENV VITE_WEBSITE_PUBLIC_ORIGIN=$VITE_WEBSITE_PUBLIC_ORIGIN
RUN npm run build
FROM nginx:stable-alpine
COPY --from=build /source/apps/management/dist/ /usr/share/nginx/html/
COPY deployment/docker/staff.conf.template /etc/nginx/templates/default.conf.template
EXPOSE 80
