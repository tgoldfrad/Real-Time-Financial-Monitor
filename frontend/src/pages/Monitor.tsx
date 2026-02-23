import { useMemo, useState } from 'react';
import { useTransactions } from '../hooks/useTransactions';
import type { TransactionStatus } from '../types/transaction';
import ConnectionIndicator from '../components/ConnectionIndicator';
import FilterBar from '../components/FilterBar';
import TransactionTable from '../components/TransactionTable';
import styles from './Monitor.module.css';

export default function Monitor() {
  const { transactions, connectionStatus, newTransactionIds } = useTransactions();
  const [filter, setFilter] = useState<TransactionStatus | 'All'>('All');

  const filtered = useMemo(() => {
    if (filter === 'All') return transactions;
    return transactions.filter((tx) => tx.status === filter);
  }, [transactions, filter]);

  const counts = useMemo(() => {
    const map: Record<string, number> = { All: transactions.length };
    for (const tx of transactions) {
      map[tx.status] = (map[tx.status] ?? 0) + 1;
    }
    return map;
  }, [transactions]);

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div>
          <h1 className={styles.title}>Live Dashboard</h1>
          <p className={styles.subtitle}>
            Transactions appear in real-time as they are ingested.
          </p>
        </div>
        <ConnectionIndicator status={connectionStatus} />
      </div>

      <div className={styles.toolbar}>
        <FilterBar current={filter} onChange={setFilter} counts={counts} />
        <span className={styles.total}>{filtered.length} transactions</span>
      </div>

      <TransactionTable transactions={filtered} newTransactionIds={newTransactionIds} />
    </div>
  );
}
