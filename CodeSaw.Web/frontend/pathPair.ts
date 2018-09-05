export interface PathPair {
    newPath: string;
    oldPath: string;
}

export type List = PathPair[];

export const emptyPathPair: PathPair = { newPath: null, oldPath: null };

export const equal = (a: PathPair, b: PathPair) => a.newPath == b.newPath;

export const contains = (list: List, path: PathPair) => {
    const idx = list.findIndex(p => p.newPath == path.newPath);

    return idx >= 0;
}

export const make = (path: string):PathPair => ( {oldPath: path, newPath: path} );