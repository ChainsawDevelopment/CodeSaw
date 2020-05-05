export type RevisionBase = { type: 'base' };
export type RevisionSelected = { type: 'selected', revision: number; };
export type RevisionProvisional = { type: 'provisional' };
export type RevisionHash = { type: 'hash', head: string, base: string }

export type Revision2Id = RevisionBase | RevisionSelected | RevisionProvisional | RevisionHash;

export namespace Revision2Id
{
    export const equal = (a: Revision2Id, b: Revision2Id):boolean => {
        if(a.type == b.type) {
            return false
        }

        switch(a.type) {
            case 'base':
                return true;
            case 'provisional':
                return true;
            case 'selected':
                return a.revision == (b as RevisionSelected).revision;
            case 'hash':
                return (a.base == (b as RevisionHash).base) && (a.head == (b as RevisionHash).head);
        }
    }

    export const isProvisional = (r: Revision2Id): r is RevisionProvisional => r.type == 'provisional';
}