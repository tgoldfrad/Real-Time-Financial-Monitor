import type { TransactionStatus } from '../types/transaction';
import styles from './StatusBadge.module.css';

interface Props {
  status: TransactionStatus;
}

const statusClass: Record<TransactionStatus, string> = {
  Completed: styles.completed,
  Pending: styles.pending,
  Failed: styles.failed,
};

export default function StatusBadge({ status }: Props) {
  return (
    <span className={`${styles.badge} ${statusClass[status] ?? ''}`}>
      {status}
    </span>
  );
}
