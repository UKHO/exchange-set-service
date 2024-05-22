output "redis_connection_string" {
  value = azurerm_redis_cache.redis_cache.0.primary_connection_string
}