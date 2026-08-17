#!/usr/bin/env bash
set -euo pipefail

dotnet_directory="${PWD}/.dotnet"
dotnet_install_script="${PWD}/dotnet-install.sh"
publish_directory="${PWD}/output"

curl -fsSL https://dot.net/v1/dotnet-install.sh -o "${dotnet_install_script}"
bash "${dotnet_install_script}" --channel 10.0 --install-dir "${dotnet_directory}"

"${dotnet_directory}/dotnet" --version
"${dotnet_directory}/dotnet" publish \
  src/Bancada.Web/Bancada.Web.csproj \
  --configuration Release \
  --output "${publish_directory}"

if [[ -n "${API_BASE_URL:-}" ]]; then
  api_base_url="${API_BASE_URL%/}/"

  if [[ "${api_base_url}" != https://* ]]; then
    echo "API_BASE_URL must be an absolute HTTPS URL." >&2
    exit 1
  fi

  printf '{\n  "ApiBaseUrl": "%s"\n}\n' "${api_base_url}" \
    > "${publish_directory}/wwwroot/appsettings.json"
else
  echo "API_BASE_URL is not set; the published client will use its local development API URL." >&2
fi
