export type RevisionBase = { type: 'base' };
export type RevisionSelected = { type: 'selected'; revision: number };
export type RevisionProvisional = { type: 'provisional' };
export type RevisionHash = { type: 'hash'; head: string }; //, base: string }

export type RevisionId = RevisionBase | RevisionSelected | RevisionProvisional | RevisionHash;

export type RemoteRevisionId = RevisionSelected | RevisionHash;
export type LocalRevisionId = RevisionBase | RevisionSelected | RevisionProvisional;

export namespace RevisionId {
    export const equal = (a: RevisionId, b: RevisionId): boolean => {
        if (a.type != b.type) {
            return false;
        }

        switch (a.type) {
            case 'base':
                return true;
            case 'provisional':
                return true;
            case 'selected':
                return a.revision == (b as RevisionSelected).revision;
            case 'hash':
                return a.head == (b as RevisionHash).head;
        }
    };

    export const Provisional: RevisionProvisional = { type: 'provisional' };
    export const Base: RevisionBase = { type: 'base' };

    export const isBase = (r: RevisionId): r is RevisionBase => r.type == 'base';
    export const isSelected = (r: RevisionId): r is RevisionSelected => r.type == 'selected';
    export const isHash = (r: RevisionId): r is RevisionHash => r.type == 'hash';
    export const isProvisional = (r: RevisionId): r is RevisionProvisional => r.type == 'provisional';

    export const mapRemoteToLocal = (r: RemoteRevisionId): LocalRevisionId => {
        if (isBase(r) || isSelected(r)) {
            return r;
        }

        if (isHash(r)) {
            return Provisional;
        }
    };

    export const mapLocalToRemote = (r: LocalRevisionId, headCommit: string): RemoteRevisionId => {
        if (isBase(r)) {
            throw new Error('Holy crap2');
        }

        if (isSelected(r)) {
            return r;
        }

        if (isProvisional(r)) {
            return { type: 'hash', head: headCommit };
        }
    };

    export const makeSelected = (r: number): RevisionSelected => ({
        type: 'selected',
        revision: r,
    });

    export const makeHash = (hash: string): RevisionHash => ({
        type: 'hash',
        head: hash,
    });

    export const asString = (r: LocalRevisionId | RemoteRevisionId): string => {
        switch (r.type) {
            case 'base':
                return 'Base';
            case 'selected':
                return `Revision(${r.revision})`;
            case 'hash':
                return `Hash(${r.head})`;
            case 'provisional':
                return 'Provisional';
        }
    };

    export const asSelected = (r: LocalRevisionId | RemoteRevisionId): RevisionSelected => {
        if (!isSelected(r)) {
            throw new Error(`Must be RevisionSelected, got: ${asString(r)}`);
        }

        return r;
    };
}
