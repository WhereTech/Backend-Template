#!/usr/bin/env bash
set -euo pipefail

SERVICE_NAME="backend-template"
SERVER_NAME="" # TODO: SSH host or ~/.ssh/config alias, for example: where-test
IMAGE_TAG="${IMAGE_TAG:-latest}"
REMOTE_CONFIG_DIR="${REMOTE_CONFIG_DIR:-/etc/${SERVICE_NAME}}"
IMAGE_NAME="${SERVICE_NAME}:${IMAGE_TAG}"
IMAGE_TAR="${SERVICE_NAME}-image.tar"

if [[ -z "${SERVER_NAME}" ]]; then
    echo "Please set SERVER_NAME in deploy.sh before deployment." >&2
    exit 1
fi

cleanup() {
    rm -f "${IMAGE_TAR}"
}
trap cleanup EXIT

docker build -t "${IMAGE_NAME}" .
docker save -o "${IMAGE_TAR}" "${IMAGE_NAME}"
echo "image size: $(du -h "${IMAGE_TAR}" | cut -f1)"

scp -C "${IMAGE_TAR}" "${SERVER_NAME}:/tmp/${IMAGE_TAR}"

ssh "${SERVER_NAME}" bash -s -- \
    "${SERVICE_NAME}" \
    "${IMAGE_NAME}" \
    "${IMAGE_TAR}" \
    "${REMOTE_CONFIG_DIR}" <<'REMOTE_SCRIPT'
set -euo pipefail

service_name="$1"
image_name="$2"
image_tar="$3"
config_dir="$4"

docker load -i "/tmp/${image_tar}"
rm -f "/tmp/${image_tar}"
docker stop "${service_name}" 2>/dev/null || true
docker rm "${service_name}" 2>/dev/null || true

docker run --detach \
    --restart=always \
    --name "${service_name}" \
    --network=host \
    -v "${config_dir}:${config_dir}:ro" \
    -v "${config_dir}/DataProtection-Keys:/root/.aspnet/DataProtection-Keys" \
    "${image_name}"
REMOTE_SCRIPT
