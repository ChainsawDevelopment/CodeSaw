import { toast, ToastId } from 'react-toastify';

export default {
    success: (message: string): ToastId =>
        toast(message, {
            type: 'success',
        }),
    error: (message: string): ToastId =>
        toast(message, {
            type: 'error',
        }),
};
