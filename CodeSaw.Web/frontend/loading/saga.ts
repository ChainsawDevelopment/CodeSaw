import { startOperationAction, stopOperationAction }  from './state'; 
import { put } from 'redux-saga/effects';

export const startOperation = () => put(startOperationAction({}));
export const stopOperation = () => put(stopOperationAction({}));