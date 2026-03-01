import { useCallback } from 'react';
import { useAppDispatch, useAppSelector } from '../store/store';
import {
  updateFormField,
  setFormData,
  submitTransaction,
  generateAndSend,
  FORM_CURRENCIES,
  FORM_STATUSES,
} from '../store/slices/transactionFormSlice';
import type { TransactionFormData } from '../types/transaction';

export function useTransactionForm() {
  const dispatch = useAppDispatch();
  const formData = useAppSelector(state => state.transactionForm.formData);
  const isSubmitting = useAppSelector(state => state.transactionForm.isSubmitting);
  const lastResult = useAppSelector(state => state.transactionForm.lastResult);

  const updateField = useCallback(<K extends keyof TransactionFormData>(
    key: K,
    value: TransactionFormData[K]
  ) => {
    dispatch(updateFormField({ key, value: value as string | number }));
  }, [dispatch]);

  const fillRandom = useCallback(() => {
    const randomAmount = Math.round((Math.random() * 9999 + 1) * 100) / 100;
    const randomCurrency = FORM_CURRENCIES[Math.floor(Math.random() * FORM_CURRENCIES.length)];
    const randomStatus = FORM_STATUSES[Math.floor(Math.random() * FORM_STATUSES.length)];
    dispatch(setFormData({ amount: randomAmount, currency: randomCurrency, status: randomStatus }));
  }, [dispatch]);

  const submit = useCallback(() => {
    dispatch(submitTransaction(formData));
  }, [dispatch, formData]);

  const handleGenerateAndSend = useCallback(() => {
    dispatch(generateAndSend());
  }, [dispatch]);

  return {
    formData,
    updateField,
    fillRandom,
    submit,
    generateAndSend: handleGenerateAndSend,
    isSubmitting,
    lastResult,
    currencies: FORM_CURRENCIES,
    statuses: FORM_STATUSES,
  };
}
