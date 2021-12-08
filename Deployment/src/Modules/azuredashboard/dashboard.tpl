{
    "lenses": {
      "0": {
        "order": 0,
        "parts": {
          "0": {
            "position": {
              "x": 0,
              "y": 0,
              "colSpan": 6,
              "rowSpan": 4
            },
            "metadata": {
              "inputs": [
                {
                  "name": "options",
                  "isOptional": true
                },
                {
                  "name": "sharedTimeRange",
                  "isOptional": true
                }
              ],
              "type": "Extension/HubsExtension/PartType/MonitorChartPart",
              "settings": {
                "content": {
                  "options": {
                    "chart": {
                      "metrics": [
                        {
                          "resourceMetadata": {
                            "id": "/subscriptions/${subscription_id}/resourceGroups/ess-${environment}-rg/providers/Microsoft.Web/sites/ess-${environment}-webapp"
                          },
                          "name": "HttpResponseTime",
                          "aggregationType": 4,
                          "namespace": "microsoft.web/sites",
                          "metricVisualization": {
                            "displayName": "Response Time",
                            "resourceDisplayName": "ess-${environment}-webapp"
                          }
                        }
                      ],
                      "title": "REQUEST LATENCY - (Avg Response Time for ess-${environment}-webapp)",
                      "titleKind": 2,
                      "visualization": {
                        "chartType": 2,
                        "legendVisualization": {
                          "isVisible": true,
                          "position": 2,
                          "hideSubtitle": false
                        },
                        "axisVisualization": {
                          "x": {
                            "isVisible": true,
                            "axisType": 2
                          },
                          "y": {
                            "isVisible": true,
                            "axisType": 1
                          }
                        },
                        "disablePinning": true
                      }
                    }
                  }
                }
              }
            }
          },
          "1": {
            "position": {
              "x": 6,
              "y": 0,
              "colSpan": 6,
              "rowSpan": 4
            },
            "metadata": {
              "inputs": [
                {
                  "name": "options",
                  "isOptional": true
                },
                {
                  "name": "sharedTimeRange",
                  "isOptional": true
                }
              ],
              "type": "Extension/HubsExtension/PartType/MonitorChartPart",
              "settings": {
                "content": {
                  "options": {
                    "chart": {
                      "metrics": [
                        {
                          "resourceMetadata": {
                            "id": "/subscriptions/${subscription_id}/resourceGroups/ess-${environment}-rg/providers/Microsoft.Web/sites/ess-${environment}-webapp"
                          },
                          "name": "BytesReceived",
                          "aggregationType": 1,
                          "namespace": "microsoft.web/sites",
                          "metricVisualization": {
                            "displayName": "Data In",
                            "resourceDisplayName": "ess-${environment}-webapp"
                          }
                        }
                      ],
                      "title": "THROUGHPUT - (Sum Data In for ess-${environment}-webapp)",
                      "titleKind": 2,
                      "visualization": {
                        "chartType": 2,
                        "legendVisualization": {
                          "isVisible": true,
                          "position": 2,
                          "hideSubtitle": false
                        },
                        "axisVisualization": {
                          "x": {
                            "isVisible": true,
                            "axisType": 2
                          },
                          "y": {
                            "isVisible": true,
                            "axisType": 1
                          }
                        },
                        "disablePinning": true
                      }
                    }
                  }
                }
              }
            }
          },
          "2": {
            "position": {
              "x": 12,
              "y": 0,
              "colSpan": 6,
              "rowSpan": 4
            },
            "metadata": {
              "inputs": [
                {
                  "name": "options",
                  "isOptional": true
                },
                {
                  "name": "sharedTimeRange",
                  "isOptional": true
                }
              ],
              "type": "Extension/HubsExtension/PartType/MonitorChartPart",
              "settings": {
                "content": {
                  "options": {
                    "chart": {
                      "metrics": [
                        {
                          "resourceMetadata": {
                            "id": "/subscriptions/${subscription_id}/resourceGroups/ess-${environment}-rg/providers/Microsoft.Web/sites/ess-${environment}-webapp"
                          },
                          "name": "BytesSent",
                          "aggregationType": 1,
                          "namespace": "microsoft.web/sites",
                          "metricVisualization": {
                            "displayName": "Data Out",
                            "resourceDisplayName": "ess-${environment}-webapp"
                          }
                        }
                      ],
                      "title": "THROUGHPUT - (Sum Data Out for ess-${environment}-webapp)",
                      "titleKind": 2,
                      "visualization": {
                        "chartType": 2,
                        "legendVisualization": {
                          "isVisible": true,
                          "position": 2,
                          "hideSubtitle": false
                        },
                        "axisVisualization": {
                          "x": {
                            "isVisible": true,
                            "axisType": 2
                          },
                          "y": {
                            "isVisible": true,
                            "axisType": 1
                          }
                        },
                        "disablePinning": true
                      }
                    }
                  }
                }
              }
            }
          },
          "3": {
            "position": {
              "x": 0,
              "y": 4,
              "colSpan": 6,
              "rowSpan": 4
            },
            "metadata": {
              "inputs": [
                {
                  "name": "options",
                  "isOptional": true
                },
                {
                  "name": "sharedTimeRange",
                  "isOptional": true
                }
              ],
              "type": "Extension/HubsExtension/PartType/MonitorChartPart",
              "settings": {
                "content": {
                  "options": {
                    "chart": {
                      "metrics": [
                        {
                          "resourceMetadata": {
                            "id": "/subscriptions/${subscription_id}/resourceGroups/ess-${environment}-rg/providers/Microsoft.Web/sites/ess-${environment}-webapp"
                          },
                          "name": "Http5xx",
                          "aggregationType": 7,
                          "namespace": "microsoft.web/sites",
                          "metricVisualization": {
                            "displayName": "Http Server Errors",
                            "resourceDisplayName": "ess-${environment}-webapp"
                          }
                        }
                      ],
                      "title": "ERROR RATE - (Count Http Server Errors for ess-${environment}-webapp)",
                      "titleKind": 2,
                      "visualization": {
                        "chartType": 2,
                        "legendVisualization": {
                          "isVisible": true,
                          "position": 2,
                          "hideSubtitle": false
                        },
                        "axisVisualization": {
                          "x": {
                            "isVisible": true,
                            "axisType": 2
                          },
                          "y": {
                            "isVisible": true,
                            "axisType": 1
                          }
                        },
                        "disablePinning": true
                      }
                    }
                  }
                }
              }
            }
          },
          "4": {
            "position": {
              "x": 6,
              "y": 4,
              "colSpan": 6,
              "rowSpan": 4
            },
            "metadata": {
              "inputs": [
                {
                  "name": "options",
                  "isOptional": true
                },
                {
                  "name": "sharedTimeRange",
                  "isOptional": true
                }
              ],
              "type": "Extension/HubsExtension/PartType/MonitorChartPart",
              "settings": {
                "content": {
                  "options": {
                    "chart": {
                      "metrics": [
                        {
                          "resourceMetadata": {
                            "id": "/subscriptions/${subscription_id}/resourceGroups/ess-${environment}-rg/providers/Microsoft.Web/sites/ess-${environment}-webapp"
                          },
                          "name": "HealthCheckStatus",
                          "aggregationType": 7,
                          "namespace": "microsoft.web/sites",
                          "metricVisualization": {
                            "displayName": "Health check status",
                            "resourceDisplayName": "ess-${environment}-webapp"
                          }
                        }
                      ],
                      "title": "AVAILABILITY - (Count Health check status for ess-${environment}-webapp)",
                      "titleKind": 2,
                      "visualization": {
                        "chartType": 2,
                        "legendVisualization": {
                          "isVisible": true,
                          "position": 2,
                          "hideSubtitle": false
                        },
                        "axisVisualization": {
                          "x": {
                            "isVisible": true,
                            "axisType": 2
                          },
                          "y": {
                            "isVisible": true,
                            "axisType": 1
                          }
                        },
                        "disablePinning": true
                      }
                    }
                  }
                }
              }
            }
          },
          "5": {
            "position": {
              "x": 12,
              "y": 4,
              "colSpan": 6,
              "rowSpan": 4
            },
            "metadata": {
              "inputs": [
                {
                  "name": "options",
                  "isOptional": true
                },
                {
                  "name": "sharedTimeRange",
                  "isOptional": true
                }
              ],
              "type": "Extension/HubsExtension/PartType/MonitorChartPart",
              "settings": {
                "content": {
                  "options": {
                    "chart": {
                      "metrics": [
                        {
                          "resourceMetadata": {
                            "id": "/subscriptions/${subscription_id}/resourceGroups/ess-${environment}-rg/providers/Microsoft.Storage/storageAccounts/ess${environment}lxsstorageukho"
                          },
                          "name": "QueueCount",
                          "aggregationType": 4,
                          "namespace": "microsoft.storage/storageaccounts/queueservices",
                          "metricVisualization": {
                            "displayName": "Queue Count"
                          }
                        },
                        {
                          "resourceMetadata": {
                            "id": "/subscriptions/${subscription_id}/resourceGroups/ess-${environment}-rg/providers/Microsoft.Storage/storageAccounts/ess${environment}mxsstorageukho"
                          },
                          "name": "QueueCount",
                          "aggregationType": 4,
                          "namespace": "microsoft.storage/storageaccounts/queueservices",
                          "metricVisualization": {
                            "displayName": "Queue Count"
                          }
                        },
                        {
                          "resourceMetadata": {
                            "id": "/subscriptions/${subscription_id}/resourceGroups/ess-${environment}-rg/providers/Microsoft.Storage/storageAccounts/ess${environment}sxsstorageukho"
                          },
                          "name": "QueueCount",
                          "aggregationType": 4,
                          "namespace": "microsoft.storage/storageaccounts/queueservices",
                          "metricVisualization": {
                            "displayName": "Queue Count"
                          }
                        }
                      ],
                      "title": "QUEUE - (Avg Queue Count for essdevlxsstorageukho/essdevmxsstorageukho/essdevsxsstorageukho)",
                      "titleKind": 2,
                      "visualization": {
                        "chartType": 2,
                        "legendVisualization": {
                          "isVisible": true,
                          "position": 2,
                          "hideSubtitle": false
                        },
                        "axisVisualization": {
                          "x": {
                            "isVisible": true,
                            "axisType": 2
                          },
                          "y": {
                            "isVisible": true,
                            "axisType": 1
                          }
                        },
                        "disablePinning": true
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
    },
    "metadata": {
      "model": {
        "timeRange": {
          "value": {
            "relative": {
              "duration": 24,
              "timeUnit": 1
            }
          },
          "type": "MsPortalFx.Composition.Configuration.ValueTypes.TimeRange"
        },
        "filterLocale": {
          "value": "en-us"
        },
        "filters": {
          "value": {
            "MsPortalFx_TimeRange": {
              "model": {
                "format": "local",
                "granularity": "auto",
                "relative": "7d"
              },
              "displayCache": {
                "name": "Local Time",
                "value": "Past 7 days"
              },
              "filteredPartIds": [
                "StartboardPart-MonitorChartPart-d21550e7-283a-4106-9cf7-9b16efcca0a0",
                "StartboardPart-MonitorChartPart-d21550e7-283a-4106-9cf7-9b16efcca0a2",
                "StartboardPart-MonitorChartPart-d21550e7-283a-4106-9cf7-9b16efcca0a4",
                "StartboardPart-MonitorChartPart-d21550e7-283a-4106-9cf7-9b16efcca0a6",
                "StartboardPart-MonitorChartPart-d21550e7-283a-4106-9cf7-9b16efcca0a8",
                "StartboardPart-MonitorChartPart-d21550e7-283a-4106-9cf7-9b16efcca0aa"
              ]
            }
          }
        }
      }
    },
  "name": "ESS-${environment}-Monitoring-Dashboard",
  "type": "Microsoft.Portal/dashboards",
  "location": "INSERT LOCATION",
  "tags": {
    "hidden-title": "ESS-${environment}-Monitoring-Dashboard"
  },
  "apiVersion": "2015-08-01-preview"
}