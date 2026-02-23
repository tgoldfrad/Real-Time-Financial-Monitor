const API_BASE = 'http://localhost:5000/api';

export async function postTransaction(data: {
  transactionId?: string;
  amount: number;
  currency: string;
  status: string;
  timestamp?: string;
}): Promise<Response> {
  return fetch(`${API_BASE}/transactions`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  });
}

export async function fetchTransactions(): Promise<Response> {
  return fetch(`${API_BASE}/transactions`);
}
