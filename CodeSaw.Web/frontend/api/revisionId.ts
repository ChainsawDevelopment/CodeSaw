export type RevisionBase = { type: 'base' };
export type RevisionSelected = { type: 'selected', revision: number; };
export type RevisionProvisional = { type: 'provisional' };
export type RevisionHash = { type: 'hash', head: string }; //, base: string }

export type Revision2Id = RevisionBase | RevisionSelected | RevisionProvisional | RevisionHash;

export type RemoteRevisionId = RevisionSelected | RevisionHash;
export type LocalRevisionId = RevisionBase | RevisionSelected | RevisionProvisional;

export namespace Revision2Id
{
    export const equal = (a: Revision2Id, b: Revision2Id):boolean => {
        if(a.type != b.type) {
            return false
        }

        switch(a.type) {
            case 'base':
                return true;
            case 'provisional':
                return true;
            case 'selected':
                return a.revision == (b as RevisionSelected).revision;
            // case 'hash':
            //     return (a.base == (b as RevisionHash).base) && (a.head == (b as RevisionHash).head);
            case 'hash':
                return (a.head == (b as RevisionHash).head);
        }
    }

    export const Provisional: RevisionProvisional = {type: 'provisional'};
    export const Base: RevisionBase = {type: 'base'};

    export const isBase = (r: Revision2Id): r is RevisionBase => r.type == 'base';
    export const isSelected = (r: Revision2Id): r is RevisionSelected => r.type == 'selected';
    export const isHash = (r: Revision2Id): r is RevisionHash => r.type == 'hash';
    export const isProvisional = (r: Revision2Id): r is RevisionProvisional => r.type == 'provisional';

    export const mapRemoteToLocal = (r: RemoteRevisionId) : LocalRevisionId => {
        if (isBase(r) || isSelected(r)) {
            return r;
        }

        if(isHash(r)) {
            return Provisional;
        }
    }

    export const makeSelected = (r: number): RevisionSelected => ({
        type: 'selected',
        revision:r
    });

    export const asString = (r: LocalRevisionId | RemoteRevisionId) => {
        switch(r.type) {
            case 'base':
                return 'Base';
            case 'selected':
                return `Revision(${r.revision})`;
            case 'hash':
                return `Hash(${r.head})`;
            case 'provisional':
                return 'Provisional';
        }
    }
}