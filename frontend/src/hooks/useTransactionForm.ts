import { useState, useCallback } from 'react';
import { postTransaction } from '../services/api';
import type { TransactionFormData, TransactionStatus } from '../types/transaction';
const CURRENCIES = ['USD', 'EUR', 'ILS', 'GBP', 'JPY', 'CHF', 'CAD', 'AUD'];
const STATUSES: TransactionStatus[] = ['Pending', 'Completed', 'Failed'];

function randomAmount(): number {
  return Math.round((Math.random() * 9999 + 1) * 100) / 100;
}

function randomItem<T>(arr: T[]): T {
  return arr[Math.floor(Math.random() * arr.length)];
}

function generateId(): string {
  return crypto.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

export function useTransactionForm() {
  const [formData, setFormData] = useState<TransactionFormData>({
    amount: 100,
    currency: 'USD',
    status: 'Pending',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [lastResult, setLastResult] = useState<{ ok: boolean; message: string } | null>(null);

  const updateField = useCallback(<K extends keyof TransactionFormData>(
    key: K,
    value: TransactionFormData[K]
  ) => {
    setFormData(prev => ({ ...prev, [key]: value }));
  }, []);

  const fillRandom = useCallback(() => {
    setFormData({
      amount: randomAmount(),
      currency: randomItem(CURRENCIES),
      status: randomItem(STATUSES),
    });
  }, []);

  const submit = useCallback(async () => {
    setIsSubmitting(true);
    setLastResult(null);
    try {
      const response = await postTransaction({
        transactionId: generateId(),
        amount: formData.amount,
        currency: formData.currency,
        status: formData.status,
        timestamp: new Date().toISOString(),
      });
      if (response.ok) {
        setLastResult({ ok: true, message: 'Transaction sent successfully!' });
      } else {
        const body = await response.json().catch(() => ({}));
        setLastResult({ ok: false, message: body.error || `Error: ${response.status}` });
      }
    } catch (err) {
      setLastResult({ ok: false, message: `Network error: ${(err as Error).message}` });
    } finally {
      setIsSubmitting(false);
    }
  }, [formData]);

  const generateAndSend = useCallback(async () => {
    const data = {
      transactionId: generateId(),
      amount: randomAmount(),
      currency: randomItem(CURRENCIES),
      status: randomItem(STATUSES),
      timestamp: new Date().toISOString(),
    };
    setFormData({ amount: data.amount, currency: data.currency, status: data.status });
    setIsSubmitting(true);
    setLastResult(null);
    try {
      const response = await postTransaction(data);
      if (response.ok) {
        setLastResult({ ok: true, message: 'Random transaction sent!' });
      } else {
        const body = await response.json().catch(() => ({}));
        setLastResult({ ok: false, message: body.error || `Error: ${response.status}` });
      }
    } catch (err) {
      setLastResult({ ok: false, message: `Network error: ${(err as Error).message}` });
    } finally {
      setIsSubmitting(false);
    }
  }, []);

  return {
    formData,
    updateField,
    fillRandom,
    submit,
    generateAndSend,
    isSubmitting,
    lastResult,
    currencies: CURRENCIES,
    statuses: STATUSES,
  };
}
