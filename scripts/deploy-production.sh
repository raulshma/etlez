#!/bin/bash

# ETL Framework Production Deployment Script
# This script deploys the ETL Framework to a production environment

set -euo pipefail

# Configuration
NAMESPACE="etl-framework"
RELEASE_NAME="etl-framework"
CHART_PATH="./helm/etl-framework"
VALUES_FILE="./helm/values-production.yaml"
IMAGE_TAG="${IMAGE_TAG:-latest}"
ENVIRONMENT="${ENVIRONMENT:-production}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    # Check if kubectl is installed and configured
    if ! command -v kubectl &> /dev/null; then
        log_error "kubectl is not installed or not in PATH"
        exit 1
    fi
    
    # Check if helm is installed
    if ! command -v helm &> /dev/null; then
        log_error "helm is not installed or not in PATH"
        exit 1
    fi
    
    # Check if we can connect to the cluster
    if ! kubectl cluster-info &> /dev/null; then
        log_error "Cannot connect to Kubernetes cluster"
        exit 1
    fi
    
    # Check if docker is available for building images
    if ! command -v docker &> /dev/null; then
        log_warning "Docker is not available - assuming image is already built"
    fi
    
    log_success "Prerequisites check passed"
}

# Create namespace if it doesn't exist
create_namespace() {
    log_info "Creating namespace if it doesn't exist..."
    
    if ! kubectl get namespace "$NAMESPACE" &> /dev/null; then
        kubectl create namespace "$NAMESPACE"
        log_success "Created namespace: $NAMESPACE"
    else
        log_info "Namespace $NAMESPACE already exists"
    fi
}

# Apply secrets and configmaps
apply_secrets() {
    log_info "Applying secrets and configmaps..."
    
    # Apply secrets from environment variables or files
    if [[ -f "./k8s/secrets-${ENVIRONMENT}.yaml" ]]; then
        kubectl apply -f "./k8s/secrets-${ENVIRONMENT}.yaml" -n "$NAMESPACE"
        log_success "Applied secrets"
    else
        log_warning "No secrets file found for environment: $ENVIRONMENT"
    fi
    
    # Apply configmaps
    if [[ -f "./k8s/configmap-${ENVIRONMENT}.yaml" ]]; then
        kubectl apply -f "./k8s/configmap-${ENVIRONMENT}.yaml" -n "$NAMESPACE"
        log_success "Applied configmaps"
    fi
}

# Build and push Docker image
build_and_push_image() {
    if command -v docker &> /dev/null; then
        log_info "Building Docker image..."
        
        # Build the image
        docker build -t "ghcr.io/your-org/etl-framework:${IMAGE_TAG}" .
        
        # Push the image
        docker push "ghcr.io/your-org/etl-framework:${IMAGE_TAG}"
        
        log_success "Built and pushed Docker image with tag: ${IMAGE_TAG}"
    else
        log_info "Skipping Docker build - assuming image is already available"
    fi
}

# Deploy using Helm
deploy_with_helm() {
    log_info "Deploying with Helm..."
    
    # Add or update Helm repositories if needed
    # helm repo add etl-framework https://your-org.github.io/etl-framework-helm
    # helm repo update
    
    # Deploy or upgrade the release
    helm upgrade --install "$RELEASE_NAME" "$CHART_PATH" \
        --namespace "$NAMESPACE" \
        --values "$VALUES_FILE" \
        --set image.tag="$IMAGE_TAG" \
        --set environment="$ENVIRONMENT" \
        --wait \
        --timeout=10m
    
    log_success "Helm deployment completed"
}

# Deploy using kubectl (alternative to Helm)
deploy_with_kubectl() {
    log_info "Deploying with kubectl..."
    
    # Apply Kubernetes manifests
    kubectl apply -f "./k8s/deployment.yml" -n "$NAMESPACE"
    
    # Wait for deployment to be ready
    kubectl rollout status deployment/etl-framework-api -n "$NAMESPACE" --timeout=600s
    
    log_success "kubectl deployment completed"
}

# Run health checks
run_health_checks() {
    log_info "Running health checks..."
    
    # Wait for pods to be ready
    kubectl wait --for=condition=ready pod -l app=etl-framework-api -n "$NAMESPACE" --timeout=300s
    
    # Get service endpoint
    SERVICE_IP=$(kubectl get service etl-framework-api-service -n "$NAMESPACE" -o jsonpath='{.status.loadBalancer.ingress[0].ip}')
    if [[ -z "$SERVICE_IP" ]]; then
        SERVICE_IP=$(kubectl get service etl-framework-api-service -n "$NAMESPACE" -o jsonpath='{.spec.clusterIP}')
    fi
    
    # Test health endpoint
    if command -v curl &> /dev/null; then
        if curl -f "http://${SERVICE_IP}/health" &> /dev/null; then
            log_success "Health check passed"
        else
            log_error "Health check failed"
            exit 1
        fi
    else
        log_warning "curl not available - skipping HTTP health check"
    fi
    
    # Check pod logs for errors
    log_info "Checking recent logs for errors..."
    if kubectl logs -l app=etl-framework-api -n "$NAMESPACE" --tail=50 | grep -i error; then
        log_warning "Errors found in logs - please review"
    else
        log_success "No errors found in recent logs"
    fi
}

# Run smoke tests
run_smoke_tests() {
    log_info "Running smoke tests..."
    
    # Port forward to access the API
    kubectl port-forward service/etl-framework-api-service 8080:80 -n "$NAMESPACE" &
    PORT_FORWARD_PID=$!
    
    # Wait for port forward to be ready
    sleep 5
    
    # Run basic API tests
    if command -v curl &> /dev/null; then
        # Test API endpoints
        if curl -f "http://localhost:8080/health" &> /dev/null; then
            log_success "Health endpoint test passed"
        else
            log_error "Health endpoint test failed"
        fi
        
        if curl -f "http://localhost:8080/api/pipelines" &> /dev/null; then
            log_success "Pipelines API test passed"
        else
            log_error "Pipelines API test failed"
        fi
    fi
    
    # Clean up port forward
    kill $PORT_FORWARD_PID 2>/dev/null || true
    
    log_success "Smoke tests completed"
}

# Rollback function
rollback() {
    log_warning "Rolling back deployment..."
    
    if command -v helm &> /dev/null && helm list -n "$NAMESPACE" | grep -q "$RELEASE_NAME"; then
        helm rollback "$RELEASE_NAME" -n "$NAMESPACE"
        log_success "Helm rollback completed"
    else
        kubectl rollout undo deployment/etl-framework-api -n "$NAMESPACE"
        log_success "kubectl rollback completed"
    fi
}

# Cleanup function
cleanup() {
    log_info "Cleaning up..."
    # Add any cleanup tasks here
}

# Main deployment function
main() {
    log_info "Starting ETL Framework production deployment..."
    log_info "Environment: $ENVIRONMENT"
    log_info "Image Tag: $IMAGE_TAG"
    log_info "Namespace: $NAMESPACE"
    
    # Set up error handling
    trap 'log_error "Deployment failed"; rollback; cleanup; exit 1' ERR
    trap 'cleanup' EXIT
    
    # Run deployment steps
    check_prerequisites
    create_namespace
    apply_secrets
    build_and_push_image
    
    # Choose deployment method
    if [[ -f "$CHART_PATH/Chart.yaml" ]]; then
        deploy_with_helm
    else
        deploy_with_kubectl
    fi
    
    run_health_checks
    run_smoke_tests
    
    log_success "ETL Framework deployment completed successfully!"
    
    # Display useful information
    log_info "Deployment Information:"
    kubectl get pods -n "$NAMESPACE" -l app=etl-framework-api
    kubectl get services -n "$NAMESPACE"
    kubectl get ingress -n "$NAMESPACE" 2>/dev/null || true
}

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --image-tag)
            IMAGE_TAG="$2"
            shift 2
            ;;
        --environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        --namespace)
            NAMESPACE="$2"
            shift 2
            ;;
        --rollback)
            rollback
            exit 0
            ;;
        --help)
            echo "Usage: $0 [OPTIONS]"
            echo "Options:"
            echo "  --image-tag TAG     Docker image tag to deploy (default: latest)"
            echo "  --environment ENV   Environment to deploy to (default: production)"
            echo "  --namespace NS      Kubernetes namespace (default: etl-framework)"
            echo "  --rollback          Rollback the last deployment"
            echo "  --help              Show this help message"
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            exit 1
            ;;
    esac
done

# Run main function
main
