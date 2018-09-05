import { startOperationAction, stopOperationAction }  from './state'; 
import { put } from '../../../node_modules/redux-saga/effects';

export const startOperation = () => put(startOperationAction({}));
export const stopOperation = () => put(stopOperationAction({}));