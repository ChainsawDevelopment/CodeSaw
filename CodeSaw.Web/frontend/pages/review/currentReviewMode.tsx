import * as React from 'react';

const CurrentReviewMode = React.createContext<'reviewer' | 'author'>(undefined);

export default CurrentReviewMode;
