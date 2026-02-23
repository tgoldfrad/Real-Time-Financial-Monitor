import type { TransactionStatus } from '../types/transaction';
import styles from './FilterBar.module.css';

interface Props {
  current: TransactionStatus | 'All';
  onChange: (value: TransactionStatus | 'All') => void;
  counts: Record<string, number>;
}

const OPTIONS: (TransactionStatus | 'All')[] = ['All', 'Pending', 'Completed', 'Failed'];

export default function FilterBar({ current, onChange, counts }: Props) {
  return (
    <div className={styles.bar}>
      {OPTIONS.map((opt) => (
        <button
          key={opt}
          className={`${styles.btn} ${current === opt ? styles.active : ''}`}
          onClick={() => onChange(opt)}
        >
          {opt}
          <span className={styles.count}>{counts[opt] ?? 0}</span>
        </button>
      ))}
    </div>
  );
}
