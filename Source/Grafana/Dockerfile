FROM grafana/grafana:latest

ENV GF_ANALYTICS_REPORTING_ENABLED=FALSE \
    GF_AUTH_ANONYMOUS_ENABLED=true \
    GF_AUTH_BASIC_ENABLED=false \
    GF_SECURITY_ADMIN_PASSWORD=admin \
    GF_SECURITY_ADMIN_USER=admin \
    GF_SECURITY_ALLOW_EMBEDDING=true \
    GF_SECURITY_DISABLE_GRAVATAR=true \
    GF_USERS_ALLOW_SIGN_UP=false \
    DATABASE_PORT=5432

USER root

USER grafana

COPY DataSource.yml /etc/grafana/provisioning/datasources/datasource.yml

EXPOSE 3000