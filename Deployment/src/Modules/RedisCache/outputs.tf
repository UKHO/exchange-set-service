output "redis_connection_string" {
  value = azurerm_redis_cache.redis_cache.primary_connection_string
}