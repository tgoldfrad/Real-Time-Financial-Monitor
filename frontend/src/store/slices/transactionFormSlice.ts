import { createAsyncThunk, createSlice, type PayloadAction } from '@reduxjs/toolkit';
import { postTransaction } from '../../services/api';
import type { TransactionFormData, TransactionStatus } from '../../types/transaction';

const CURRENCIES = ['USD', 'EUR', 'ILS', 'GBP', 'JPY', 'CHF', 'CAD', 'AUD'];
const STATUSES: TransactionStatus[] = ['Pending', 'Completed', 'Failed'];

function randomAmount(): number {
  return Math.round((Math.random() * 9999 + 1) * 100) / 100;
}

function randomItem<T>(arr: T[]): T {
  return arr[Math.floor(Math.random() * arr.length)];
}

function generateId(): string {
  return crypto.randomUUID?.() ?? `${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

interface TransactionFormState {
  formData: TransactionFormData;
  isSubmitting: boolean;
  lastResult: { ok: boolean; message: string } | null;
}

const initialState: TransactionFormState = {
  formData: {
    amount: 100,
    currency: 'USD',
    status: 'Pending',
  },
  isSubmitting: false,
  lastResult: null,
};

export const submitTransaction = createAsyncThunk(
  'transactionForm/submit',
  async (formData: TransactionFormData, thunkAPI) => {
    try {
      const response = await postTransaction({
        transactionId: generateId(),
        amount: formData.amount,
        currency: formData.currency,
        status: formData.status,
        timestamp: new Date().toISOString(),
      });
      if (response.ok) {
        return { ok: true, message: 'Transaction sent successfully!' };
      }
      const body = await response.json().catch(() => ({}));
      const msg = body.error
        || body.errors?.map((e: { message: string }) => e.message).join(', ')
        || `Error: ${response.status}`;
      return thunkAPI.rejectWithValue(msg);
    } catch (e) {
      return thunkAPI.rejectWithValue(`Network error: ${(e as Error).message}`);
    }
  }
);

export const generateAndSend = createAsyncThunk(
  'transactionForm/generateAndSend',
  async (_, thunkAPI) => {
    try {
      const data = {
        transactionId: generateId(),
        amount: randomAmount(),
        currency: randomItem(CURRENCIES),
        status: randomItem(STATUSES),
        timestamp: new Date().toISOString(),
      };
      const response = await postTransaction(data);
      if (response.ok) {
        return { ok: true, message: 'Random transaction sent!', formData: { amount: data.amount, currency: data.currency, status: data.status } };
      }
      const body = await response.json().catch(() => ({}));
      const msg = body.error
        || body.errors?.map((e: { message: string }) => e.message).join(', ')
        || `Error: ${response.status}`;
      return thunkAPI.rejectWithValue(msg);
    } catch (e) {
      return thunkAPI.rejectWithValue(`Network error: ${(e as Error).message}`);
    }
  }
);

const transactionFormSlice = createSlice({
  name: 'transactionForm',
  initialState,
  reducers: {
    updateFormField(state, action: PayloadAction<{ key: keyof TransactionFormData; value: string | number }>) {
      (state.formData as Record<string, string | number>)[action.payload.key] = action.payload.value;
    },

    setFormData(state, action: PayloadAction<TransactionFormData>) {
      state.formData = action.payload;
    },

    clearLastResult(state) {
      state.lastResult = null;
    },
  },

  extraReducers: (builder) => {
    builder
      .addCase(submitTransaction.pending, (state) => {
        state.isSubmitting = true;
        state.lastResult = null;
      })
      .addCase(submitTransaction.fulfilled, (state, action) => {
        state.isSubmitting = false;
        state.lastResult = action.payload;
      })
      .addCase(submitTransaction.rejected, (state, action) => {
        state.isSubmitting = false;
        state.lastResult = { ok: false, message: action.payload as string };
      })

      .addCase(generateAndSend.pending, (state) => {
        state.isSubmitting = true;
        state.lastResult = null;
      })
      .addCase(generateAndSend.fulfilled, (state, action) => {
        state.isSubmitting = false;
        state.lastResult = { ok: action.payload.ok, message: action.payload.message };
        state.formData = action.payload.formData;
      })
      .addCase(generateAndSend.rejected, (state, action) => {
        state.isSubmitting = false;
        state.lastResult = { ok: false, message: action.payload as string };
      });
  },
});

export const { updateFormField, setFormData, clearLastResult } = transactionFormSlice.actions;

export const FORM_CURRENCIES = CURRENCIES;
export const FORM_STATUSES = STATUSES;

export default transactionFormSlice;
