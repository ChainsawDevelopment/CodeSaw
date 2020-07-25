import { startOperationAction, stopOperationAction, setOperationMessageAction } from './state';
import { put, PutEffect } from 'redux-saga/effects';
import { Action } from 'typescript-fsa';

export const startOperation = (message?: string): PutEffect<Action<{ message?: string }>> =>
    put(startOperationAction({ message }));
export const setOperationMessage = (message?: string): PutEffect<Action<{ message?: string }>> =>
    put(setOperationMessageAction({ message }));
export const stopOperation = (): PutEffect<Action<{}>> => put(stopOperationAction({}));
