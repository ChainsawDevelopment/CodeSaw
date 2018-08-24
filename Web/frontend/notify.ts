import { toast } from 'react-toastify';
import { call } from 'redux-saga/effects';

export default {
    success: (message: string) => call(() => toast(message, { 
        type: 'success'
    }))
}