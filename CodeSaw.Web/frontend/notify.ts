import { toast } from 'react-toastify';

export default {
    success: (message: string) => toast(message, { 
        type: 'success'
    }),
    error: (message: string) => toast(message, {
        type: 'error'
    })
}