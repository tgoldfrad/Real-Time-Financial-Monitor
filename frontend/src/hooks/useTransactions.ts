import { useEffect, useRef, useState, useCallback } from 'react';
import { startConnection, stopConnection, getConnection } from '../services/signalr';
import { fetchTransactions } from '../services/api';
import type { Transaction } from '../types/transaction';

const ANIMATION_DURATION_MS = 2000;

export function useTransactions() {
  const [transactions, setTransactions] = useState<Transaction[]>([]);
  const [connectionStatus, setConnectionStatus] = useState<'connecting' | 'connected' | 'disconnected'>('disconnected');
  const [newTransactionIds, setNewTransactionIds] = useState<Set<string>>(new Set());
  const pendingRef = useRef<Transaction[]>([]);
  const rafRef = useRef<number | null>(null);
  const timersRef = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map());

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

  const flush = useCallback(() => {
    rafRef.current = null;
    if (pendingRef.current.length === 0) return;
    const batch = [...pendingRef.current];
    pendingRef.current = [];
    setTransactions(prev => [...batch, ...prev]);

    const ids = batch.map(tx => tx.transactionId);
    setNewTransactionIds(prev => {
      const next = new Set(prev);
      for (const id of ids) next.add(id);
      return next;
    });
    for (const id of ids) scheduleRemoval(id);
  }, [scheduleRemoval]);

  const enqueue = useCallback((tx: Transaction) => {
    pendingRef.current.push(tx);
    if (rafRef.current === null) {
      rafRef.current = requestAnimationFrame(flush);
    }
  }, [flush]);

  useEffect(() => {
    let mounted = true;

    async function init() {
      try {
        const res = await fetchTransactions();
        if (res.ok && mounted) {
          const data: Transaction[] = await res.json();
          setTransactions(data);
        }
      } catch (err) {
        console.warn('Failed to fetch transactions:', err);
      }

      setConnectionStatus('connecting');
      try {
        const conn = getConnection();

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
      for (const timer of timersRef.current.values()) clearTimeout(timer);
      timersRef.current.clear();
      const conn = getConnection();
      conn.off('ReceiveTransaction');
      stopConnection();
    };
  }, [enqueue]);

  return { transactions, connectionStatus, newTransactionIds };
}
