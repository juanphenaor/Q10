
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL

if (!API_BASE_URL) {
  throw new Error('La variable VITE_API_BASE_URL no está configurada.')
}

export async function getOrders(page = 1, pageSize = 20) {
  const response = await fetch(
    `${API_BASE_URL}/orders?page=${page}&pageSize=${pageSize}`,
  )

  if (!response.ok) {
    throw new Error(`No se pudieron consultar los pedidos (${response.status}).`)
  }

  return response.json()
}

export async function createOrder(orderData) {
  const response = await fetch(`${API_BASE_URL}/orders`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(orderData),
  })

  if (!response.ok) {
    const errorBody = await response.json().catch(() => null)
    throw new Error(
      errorBody?.detail ??
      errorBody?.title ??
      `No se pudo crear el pedido (${response.status}).`,
    )
  }

  return response.json()
}
