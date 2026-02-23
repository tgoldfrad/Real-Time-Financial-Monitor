import { useEffect, useRef, useState, useCallback } from 'react';
import { startConnection, stopConnection, getConnection } from '../services/signalr';
import { fetchTransactions } from '../services/api';
import type { Transaction } from '../types/transaction';

const ANIMATION_DURATION_MS = 2000;

/**
 * Hook that manages the live transaction feed.
 * - Fetches initial transactions via GET.
 * - Connects to SignalR and prepends new transactions in real-time.
 * - Batches rapid updates via requestAnimationFrame to avoid freezing the UI.
 * - Tracks which transaction IDs are "new" (for entry animations).
 */
export function useTransactions() {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<'connecting' | 'connected' | 'disconnected'>('disconnected');
  const [newTransactionIds, setNewTransactionIds] = useState<Set<string>>(new Set());
  const pendingRef = useRef<Transaction[]>([]);
  const rafRef = useRef<number | null>(null);
  const timersRef = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map());

  // Remove an ID from the "new" set after the animation duration
  const scheduleRemoval = useCallback((id: string) => {
    const timer = setTimeout(() => {
      setNewTransactionIds(prev => {
        const next = new Set(prev);
        next.delete(id);
        return next;
      });
      timersRef.current.delete(id);
    }, ANIMATION_DURATION_MS);
    timersRef.current.set(id, timer);
  }, []);

  // Flush pending transactions into state in one batch
  const flush = useCallback(() => {
    rafRef.current = null;
    if (pendingRef.current.length === 0) return;
    const batch = [...pendingRef.current];
    pendingRef.current = [];
    setTransactions(prev => [...batch, ...prev]);

    // Mark these IDs as new and schedule their removal
    const ids = batch.map(tx => tx.transactionId);
    setNewTransactionIds(prev => {
      const next = new Set(prev);
      for (const id of ids) next.add(id);
      return next;
    });
    for (const id of ids) scheduleRemoval(id);
  }, [scheduleRemoval]);

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
      // Clear all pending animation timers
      for (const timer of timersRef.current.values()) clearTimeout(timer);
      timersRef.current.clear();
      const conn = getConnection();
      conn.off('ReceiveTransaction');
      stopConnection();
    };
  }, [enqueue]);

  return { transactions, connectionStatus, newTransactionIds };
}
