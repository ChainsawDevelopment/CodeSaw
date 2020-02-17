import { startOperationAction, stopOperationAction, setOperationMessageAction }  from './state'; 
import { put } from 'redux-saga/effects';

export const startOperation = (message?: string) => put(startOperationAction({ message }));
export const setOperationMessage = (message?: string) => put(setOperationMessageAction({ message }));
export const stopOperation = () => put(stopOperationAction({}));