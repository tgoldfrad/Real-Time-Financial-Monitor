import { useEffect, useRef, useState, useCallback } from 'react';
import { startConnection, stopConnection, getConnection } from '../services/signalr';
import { fetchTransactions } from '../services/api';
import type { Transaction } from '../types/transaction';

/**
 * Hook that manages the live transaction feed.
 * - Fetches initial transactions via GET.
 * - Connects to SignalR and prepends new transactions in real-time.
 * - Batches rapid updates via requestAnimationFrame to avoid freezing the UI.
 */
export function useTransactions() {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<'connecting' | 'connected' | 'disconnected'>('disconnected');
  const pendingRef = useRef<Transaction[]>([]);
  const rafRef = useRef<number | null>(null);

  // Flush pending transactions into state in one batch
  const flush = useCallback(() => {
    rafRef.current = null;
    if (pendingRef.current.length === 0) return;
    const batch = [...pendingRef.current];
    pendingRef.current = [];
    setTransactions(prev => [...batch, ...prev]);
  }, []);

  // Queue a transaction and schedule a flush on next animation frame
  const enqueue = useCallback((tx: Transaction) => {
    pendingRef.current.push(tx);
    if (rafRef.current === null) {
      rafRef.current = requestAnimationFrame(flush);
    }
  }, [flush]);

  useEffect(() => {
    let mounted = true;

    async function init() {
      // 1. Fetch existing transactions
      try {
        const res = await fetchTransactions();
        if (res.ok && mounted) {
          const data: Transaction[] = await res.json();
          setTransactions(data);
        }
      } catch (err) {
        console.warn('Failed to fetch transactions:', err);
      }

      // 2. Connect to SignalR
      setConnectionStatus('connecting');
      try {
        const conn = getConnection();

        // Register handlers BEFORE starting to avoid missing messages
        conn.on('ReceiveTransaction', (tx: Transaction) => {
          if (mounted) enqueue(tx);
        });
        conn.onreconnecting(() => mounted && setConnectionStatus('connecting'));
        conn.onreconnected(() => mounted && setConnectionStatus('connected'));
        conn.onclose(() => mounted && setConnectionStatus('disconnected'));

        await startConnection();
        if (!mounted) return;
        setConnectionStatus('connected');
      } catch {
        if (mounted) setConnectionStatus('disconnected');
      }
    }

    init();

    return () => {
      mounted = false;
      if (rafRef.current !== null) cancelAnimationFrame(rafRef.current);
      const conn = getConnection();
      conn.off('ReceiveTransaction');
      stopConnection();
    };
  }, [enqueue]);

  return { transactions, connectionStatus };
}
