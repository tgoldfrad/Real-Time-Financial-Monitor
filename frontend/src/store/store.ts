import { combineSlices, configureStore } from '@reduxjs/toolkit';
import { useDispatch, useSelector } from 'react-redux';
import transactionsSlice from './slices/transactionsSlice';
import transactionFormSlice from './slices/transactionFormSlice';

const store = configureStore({
  reducer: combineSlices(transactionsSlice, transactionFormSlice),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;

export const useAppDispatch = useDispatch.withTypes<AppDispatch>();
export const useAppSelector = useSelector.withTypes<RootState>();

export default store;
