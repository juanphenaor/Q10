
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL

if (!API_BASE_URL) {
  throw new Error('La variable VITE_API_BASE_URL no está configurada.')
}

const HUB_URL = `${API_BASE_URL.replace(/\/api\/?$/, '')}/hubs/orders`

export function createOrdersHubConnection() {
  return new HubConnectionBuilder()
    .withUrl(HUB_URL)
    .withAutomaticReconnect()
    .configureLogging(LogLevel.Information)
    .build()
}
