import styles from './ConnectionIndicator.module.css';

interface Props {
  status: 'connecting' | 'connected' | 'disconnected';
}

const labels: Record<Props['status'], string> = {
  connected: 'Connected',
  connecting: 'Reconnecting…',
  disconnected: 'Disconnected',
};

export default function ConnectionIndicator({ status }: Props) {
  return (
    <span className={`${styles.indicator} ${styles[status]}`}>
      <span className={styles.dot} />
      {labels[status]}
    </span>
  );
}
