import { createAsyncThunk, createSlice, type PayloadAction } from '@reduxjs/toolkit';
import { fetchTransactions } from '../../services/api';
import type { Transaction } from '../../types/transaction';

type ConnectionStatus = 'connecting' | 'connected' | 'disconnected';

interface TransactionsState {
  items: Transaction[];
  connectionStatus: ConnectionStatus;
  newTransactionIds: string[];
  loading: boolean;
  error: string | null;
}

const initialState: TransactionsState = {
  items: [],
  connectionStatus: 'disconnected',
  newTransactionIds: [],
  loading: false,
  error: null,
};

export const loadTransactions = createAsyncThunk(
  'transactions/load',
  async (_, thunkAPI) => {
    try {
      const res = await fetchTransactions();
      if (!res.ok) throw new Error(`Server error: ${res.status}`);
      return (await res.json()) as Transaction[];
    } catch (e) {
      return thunkAPI.rejectWithValue((e as Error).message);
    }
  }
);

const transactionsSlice = createSlice({
  name: 'transactions',
  initialState,
  reducers: {
    addTransactions(state, action: PayloadAction<Transaction[]>) {
      const existingIds = new Set(state.items.map(t => t.transactionId));
      const uniqueNew = action.payload.filter(t => !existingIds.has(t.transactionId));
      state.items = [...uniqueNew.reverse(), ...state.items];
    },

    setConnectionStatus(state, action: PayloadAction<ConnectionStatus>) {
      state.connectionStatus = action.payload;
    },

    addNewIds(state, action: PayloadAction<string[]>) {
      for (const id of action.payload) {
        if (!state.newTransactionIds.includes(id)) {
          state.newTransactionIds.push(id);
        }
      }
    },

    removeNewId(state, action: PayloadAction<string>) {
      state.newTransactionIds = state.newTransactionIds.filter(id => id !== action.payload);
    },
  },

  extraReducers: (builder) => {
    builder
      .addCase(loadTransactions.pending, (state) => {
        state.loading = true;
        state.error = null;
      })
      .addCase(loadTransactions.fulfilled, (state, action) => {
        state.loading = false;
        state.items = action.payload;
      })
      .addCase(loadTransactions.rejected, (state, action) => {
        state.loading = false;
        state.error = action.payload as string;
      });
  },
});

export const {
  addTransactions,
  setConnectionStatus,
  addNewIds,
  removeNewId,
} = transactionsSlice.actions;

export default transactionsSlice;
