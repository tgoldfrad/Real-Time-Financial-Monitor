export type TransactionStatus = 'Pending' | 'Completed' | 'Failed';

export interface Transaction {
  transactionId: string;
  amount: number;
  currency: string;
  status: TransactionStatus;
  timestamp: string;
}

export interface TransactionFormData {
  amount: number;
  currency: string;
  status: TransactionStatus;
}
