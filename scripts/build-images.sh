#!/bin/bash

# Build Docker images for Joby
set -e

VERSION=${1:-latest}
REGISTRY=${REGISTRY:-""}

echo "=== Building Joby Docker Images ==="
echo "Version: $VERSION"

# Build backend
echo -e "\nBuilding backend image..."
docker build -t joby-backend:$VERSION ./backend

# Build frontend
echo -e "\nBuilding frontend image..."
docker build -t joby-frontend:$VERSION ./frontend

# Tag for registry if specified
if [ -n "$REGISTRY" ]; then
    echo -e "\nTagging images for registry: $REGISTRY"
    docker tag joby-backend:$VERSION $REGISTRY/joby-backend:$VERSION
    docker tag joby-frontend:$VERSION $REGISTRY/joby-frontend:$VERSION
    
    echo -e "\nPushing images to registry..."
    docker push $REGISTRY/joby-backend:$VERSION
    docker push $REGISTRY/joby-frontend:$VERSION
fi

echo -e "\n=== Build Complete ==="
echo "Images built:"
echo "  - joby-backend:$VERSION"
echo "  - joby-frontend:$VERSION"




