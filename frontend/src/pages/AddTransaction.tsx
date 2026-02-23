import { useTransactionForm } from '../hooks/useTransactionForm';
import styles from './AddTransaction.module.css';

export default function AddTransaction() {
  const {
    formData,
    updateField,
    fillRandom,
    submit,
    generateAndSend,
    isSubmitting,
    lastResult,
    currencies,
    statuses,
  } = useTransactionForm();

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>Transaction Simulator</h1>
      <p className={styles.subtitle}>
        Create mock transactions and send them to the backend ingestion API.
      </p>

      <div className={styles.card}>
        <div className={styles.field}>
          <label className={styles.label}>Amount</label>
          <input
            type="number"
            min="0.01"
            step="0.01"
            className={styles.input}
            value={formData.amount}
            onChange={(e) => updateField('amount', parseFloat(e.target.value) || 0)}
          />
        </div>

        <div className={styles.field}>
          <label className={styles.label}>Currency</label>
          <select
            className={styles.select}
            value={formData.currency}
            onChange={(e) => updateField('currency', e.target.value)}
          >
            {currencies.map((c) => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
        </div>

        <div className={styles.field}>
          <label className={styles.label}>Status</label>
          <select
            className={styles.select}
            value={formData.status}
            onChange={(e) => updateField('status', e.target.value as typeof formData.status)}
          >
            {statuses.map((s) => (
              <option key={s} value={s}>{s}</option>
            ))}
          </select>
        </div>

        <div className={styles.actions}>
          <button
            className={styles.btnSecondary}
            onClick={fillRandom}
            disabled={isSubmitting}
          >
            🎲 Fill Random
          </button>
          <button
            className={styles.btnPrimary}
            onClick={submit}
            disabled={isSubmitting}
          >
            📤 Send
          </button>
          <button
            className={styles.btnAccent}
            onClick={generateAndSend}
            disabled={isSubmitting}
          >
            ⚡ Generate &amp; Send
          </button>
        </div>

        {lastResult && (
          <div className={`${styles.toast} ${lastResult.ok ? styles.toastOk : styles.toastErr}`}>
            {lastResult.message}
          </div>
        )}
      </div>
    </div>
  );
}
