import type { Transaction } from '../types/transaction';
import StatusBadge from './StatusBadge';
import styles from './TransactionTable.module.css';

interface Props {
  transactions: Transaction[];
  newTransactionIds?: Set<string>;
}

function formatTimestamp(ts: string): string {
  return new Date(ts).toLocaleString();
}

function truncateId(id: string): string {
  return id.length > 12 ? `${id.slice(0, 8)}...` : id;
}

export default function TransactionTable({ transactions, newTransactionIds }: Props) {
  if (transactions.length === 0) {
    return <p className={styles.empty}>No transactions yet. Go to Simulator to create some!</p>;
  }

  return (
    <div className={styles.wrapper}>
      <table className={styles.table}>
        <thead>
          <tr>
            <th>ID</th>
            <th>Amount</th>
            <th>Currency</th>
            <th>Status</th>
            <th>Timestamp</th>
          </tr>
        </thead>
        <tbody>
          {transactions.map((tx) => {
            const isNew = newTransactionIds?.has(tx.transactionId);
            return (
              <tr
                key={tx.transactionId}
                className={isNew ? styles.newRow : undefined}
              >
                <td title={tx.transactionId} className={styles.mono}>
                  {truncateId(tx.transactionId)}
                </td>
                <td className={styles.amount}>
                  {tx.amount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                </td>
                <td>{tx.currency}</td>
                <td><StatusBadge status={tx.status} /></td>
                <td className={styles.time}>{formatTimestamp(tx.timestamp)}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
