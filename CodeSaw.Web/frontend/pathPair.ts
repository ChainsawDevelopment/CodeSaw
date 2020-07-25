export interface PathPair {
    newPath: string;
    oldPath: string;
}

export type List = PathPair[];

export const equal = (a: PathPair, b: PathPair): boolean => a.newPath == b.newPath;

export const contains = (list: List, path: PathPair): boolean => {
    const idx = list.findIndex((p) => p.newPath == path.newPath);

    return idx >= 0;
};

export const make = (path: string): PathPair => ({ oldPath: path, newPath: path });

export const subtract = (a: PathPair[], b: PathPair[]): PathPair[] => {
    return a.filter((itemA) => b.filter((itemB) => equal(itemA, itemB)).length == 0);
};
