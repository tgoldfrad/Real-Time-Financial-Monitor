import { useEffect, useRef, useCallback, useMemo } from 'react';
import { startConnection, stopConnection, getConnection } from '../services/signalr';
import { useAppDispatch, useAppSelector } from '../store/store';
import { loadTransactions, addTransactions, setConnectionStatus, addNewIds, removeNewId } from '../store/slices/transactionsSlice';
import type { Transaction } from '../types/transaction';

const ANIMATION_DURATION_MS = 2000;

export function useTransactions() {
  const dispatch = useAppDispatch();
  const transactions = useAppSelector(state => state.transactions.items);
  const connectionStatus = useAppSelector(state => state.transactions.connectionStatus);
  const loading = useAppSelector(state => state.transactions.loading);
  const error = useAppSelector(state => state.transactions.error);
  const newTransactionIdsArray = useAppSelector(state => state.transactions.newTransactionIds);
  const newTransactionIds = useMemo(() => new Set(newTransactionIdsArray), [newTransactionIdsArray]);

  const pendingRef = useRef<Transaction[]>([]);
  const rafRef = useRef<number | null>(null);
  const timersRef = useRef<Map<string, ReturnType<typeof setTimeout>>>(new Map());

  const scheduleRemoval = useCallback((id: string) => {
    const timer = setTimeout(() => {
      dispatch(removeNewId(id));
      timersRef.current.delete(id);
    }, ANIMATION_DURATION_MS);
    timersRef.current.set(id, timer);
  }, [dispatch]);

  const flush = useCallback(() => {
    rafRef.current = null;
    if (pendingRef.current.length === 0) return;
    const batch = [...pendingRef.current];
    pendingRef.current = [];

    dispatch(addTransactions(batch));

    const ids = batch.map(tx => tx.transactionId);
    dispatch(addNewIds(ids));
    for (const id of ids) scheduleRemoval(id);
  }, [dispatch, scheduleRemoval]);

  const enqueue = useCallback((tx: Transaction) => {
    pendingRef.current.push(tx);
    if (rafRef.current === null) {
      rafRef.current = requestAnimationFrame(flush);
    }
  }, [flush]);

  useEffect(() => {
    let mounted = true;

    dispatch(loadTransactions());

    dispatch(setConnectionStatus('connecting'));
    async function initSignalR() {
      const conn = getConnection();

      conn.off('ReceiveTransaction');
      conn.on('ReceiveTransaction', (tx: Transaction) => {
        if (mounted) enqueue(tx);
      });
      conn.onreconnecting(() => mounted && dispatch(setConnectionStatus('connecting')));
      conn.onreconnected(() => mounted && dispatch(setConnectionStatus('connected')));
      conn.onclose(() => mounted && dispatch(setConnectionStatus('disconnected')));

      const MAX_RETRIES = 3;
      for (let attempt = 0; attempt < MAX_RETRIES; attempt++) {
        try {
          await startConnection();
          if (!mounted) return;
          dispatch(setConnectionStatus('connected'));
          return;
        } catch {
          if (!mounted) return;
          dispatch(setConnectionStatus('disconnected'));
          if (attempt < MAX_RETRIES - 1) {
            await new Promise(resolve => setTimeout(resolve, 3000));
          }
        }
      }
    }

    initSignalR();

    return () => {
      mounted = false;
      if (rafRef.current !== null) cancelAnimationFrame(rafRef.current);
      for (const timer of timersRef.current.values()) clearTimeout(timer);
      timersRef.current.clear();
      const conn = getConnection();
      conn.off('ReceiveTransaction');
      stopConnection();
    };
  }, [dispatch, enqueue]);

  return { transactions, connectionStatus, newTransactionIds, loading, error };
}
