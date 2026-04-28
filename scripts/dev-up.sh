#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SQL_CONTAINER_NAME="northwind-sql"
SA_PASSWORD="Northwind_123Strong!"
SQL_IMAGE="mcr.microsoft.com/azure-sql-edge:latest"
SQL_TOOLS_IMAGE="mcr.microsoft.com/mssql-tools:latest"
SQL_SERVER="127.0.0.1,1433"
SETUP_ONLY=false
FORCE_SEED=false

for arg in "$@"; do
  case "$arg" in
    --setup-only)
      SETUP_ONLY=true
      ;;
    --force-seed)
      FORCE_SEED=true
      ;;
    *)
      echo "Unknown argument: $arg"
      echo "Usage: ./scripts/dev-up.sh [--setup-only] [--force-seed]"
      exit 1
      ;;
  esac
done

log() {
  printf "\n==> %s\n" "$1"
}

have_cmd() {
  command -v "$1" >/dev/null 2>&1
}

ensure_brew() {
  if ! have_cmd brew; then
    echo "Homebrew is required for auto-installation but is not installed."
    echo "Install Homebrew from https://brew.sh and re-run this script."
    exit 1
  fi
}

ensure_dotnet() {
  if have_cmd dotnet; then
    return
  fi

  log "Installing .NET SDK via Homebrew"
  ensure_brew
  brew install --cask dotnet-sdk
}

ensure_docker_cli() {
  if have_cmd docker; then
    return
  fi

  log "Installing Docker Desktop via Homebrew"
  ensure_brew
  brew install --cask docker
}

wait_for_docker() {
  local tries=0
  until docker info >/dev/null 2>&1; do
    tries=$((tries + 1))
    if [ "$tries" -ge 60 ]; then
      echo "Docker daemon did not become ready in time."
      echo "Please ensure Docker Desktop (or Colima) is running, then re-run."
      exit 1
    fi
    sleep 2
  done
}

start_docker_daemon() {
  if docker info >/dev/null 2>&1; then
    return
  fi

  if have_cmd open; then
    log "Starting Docker Desktop"
    open -a Docker >/dev/null 2>&1 || true
    wait_for_docker
    return
  fi

  if have_cmd colima; then
    log "Starting Colima"
    colima start
    wait_for_docker
    return
  fi

  echo "Docker is installed but daemon is not running."
  echo "Start Docker Desktop or install Colima, then re-run."
  exit 1
}

container_exists() {
  docker ps -a --format '{{.Names}}' | awk -v n="$SQL_CONTAINER_NAME" '$0 == n { found=1 } END { exit(found ? 0 : 1) }'
}

container_running() {
  docker ps --format '{{.Names}}' | awk -v n="$SQL_CONTAINER_NAME" '$0 == n { found=1 } END { exit(found ? 0 : 1) }'
}

start_sql_container() {
  if container_exists; then
    if ! container_running; then
      log "Starting existing SQL container: $SQL_CONTAINER_NAME"
      docker start "$SQL_CONTAINER_NAME" >/dev/null
    else
      log "SQL container already running: $SQL_CONTAINER_NAME"
    fi
    return
  fi

  log "Creating SQL container: $SQL_CONTAINER_NAME"
  docker run -d --name "$SQL_CONTAINER_NAME" \
    -e "ACCEPT_EULA=Y" \
    -e "MSSQL_SA_PASSWORD=$SA_PASSWORD" \
    -e "MSSQL_PID=Developer" \
    -p 1433:1433 \
    "$SQL_IMAGE" >/dev/null
}

sqlcmd() {
  docker run --rm --network host "$SQL_TOOLS_IMAGE" \
    /opt/mssql-tools/bin/sqlcmd -b -S "$SQL_SERVER" -U sa -P "$SA_PASSWORD" "$@"
}

wait_for_sql() {
  log "Waiting for SQL Server to accept connections"
  local tries=0
  until sqlcmd -Q "SELECT 1" >/dev/null 2>&1; do
    tries=$((tries + 1))
    if [ "$tries" -ge 60 ]; then
      echo "SQL Server did not become ready in time."
      echo "Inspect container logs with: docker logs $SQL_CONTAINER_NAME"
      exit 1
    fi
    sleep 2
  done
}

northwind_db_exists() {
  local result
  result="$(sqlcmd -h -1 -W -Q "SET NOCOUNT ON;SELECT CASE WHEN DB_ID('Northwind') IS NULL THEN 0 ELSE 1 END;")"
  [ "$result" = "1" ]
}

northwind_seed_present() {
  local result
  result="$(sqlcmd -d Northwind -h -1 -W -Q "SET NOCOUNT ON;SELECT CASE WHEN OBJECT_ID('dbo.Customers','U') IS NULL THEN 0 ELSE 1 END;")"
  [ "$result" = "1" ]
}

ensure_northwind_database() {
  if ! northwind_db_exists; then
    log "Creating Northwind database"
    sqlcmd -Q "IF DB_ID('Northwind') IS NULL CREATE DATABASE Northwind;"
  fi

  if [ "$FORCE_SEED" = true ] || ! northwind_seed_present; then
    log "Loading Northwind schema/data from setup script"
    docker run --rm --network host -v "$ROOT_DIR/setup/northwind:/scripts" "$SQL_TOOLS_IMAGE" \
      /opt/mssql-tools/bin/sqlcmd -b -S "$SQL_SERVER" -d Northwind -U sa -P "$SA_PASSWORD" \
      -i /scripts/instnwnd.sql
  else
    log "Northwind schema/data already present"
  fi
}

run_api() {
  log "Starting backend API"
  cd "$ROOT_DIR"
  dotnet run --project NorthWindTraders/NorthWindTraders.csproj
}

main() {
  ensure_dotnet
  ensure_docker_cli
  start_docker_daemon
  start_sql_container
  wait_for_sql
  ensure_northwind_database

  if [ "$SETUP_ONLY" = true ]; then
    log "Setup completed successfully"
    exit 0
  fi

  run_api
}

main
