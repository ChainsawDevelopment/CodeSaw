import { expect } from 'chai';
import * as fc from 'fast-check';
import { listFiles, buildTree, sortTree, FolderTreeNode, shortTree } from '@src/treeNode';
import * as Arb from './fileTree.arb';

fc.configureGlobal({
    numRuns: 1000
});

const printTree = <T>(root: FolderTreeNode<T>, indent: string = '') => {
    console.log(`${indent}[fold] ${root.nestElements.join('/')} ${root.name}`);

    for (const folder of root.folders) {
        printTree(folder, indent + '  ');
    }

    for (const file of root.files) {
        console.log(`${indent}[file] ${file.name}`);
    }
}

describe('Build file tree', () => {
    it('Built tree includes all files', () => {
        fc.assert(fc.property(
            Arb.fileList,
            inputFileList => {
                const builtTree = buildTree(inputFileList, f => f);
                const actualFileList = listFiles('', builtTree);

                expect(actualFileList).to.have.members(inputFileList);
                expect(actualFileList.length).to.be.equal(inputFileList.length);
            }
        ));
    });

    it('All names in built tree are splitted', () => {
        fc.assert(fc.property(
            Arb.fileList,
            inputFileList => {
                const builtTree = buildTree(inputFileList, f => f);

                Arb.visitTree(builtTree, (node, nest) => {
                    expect(node.name).to.not.contain('/');
                    nest();
                });
            }
        ));
    });

    it('Children in tree are sorted', () => {
        fc.assert(fc.property(
            Arb.fileList,
            inputFileList => {
                const builtTree = buildTree(inputFileList, f => f);
                const sortedTree = sortTree(builtTree);

                Arb.visitTree(sortedTree, (node, nest) => {
                    nest();

                    const folderNames = node.folders.map(f => f.name);
                    const fileNames = node.files.map(f => f.name);

                    expect(folderNames).to.deep.equal([...folderNames].sort());
                    expect(fileNames).to.deep.equal([...fileNames].sort());
                });
            }
        ));
    });

    it('Combine single-item folders', () => {
        fc.assert(fc.property(
            Arb.fileList,
            inputFileList => {
                const builtTree = buildTree(inputFileList, f => f);
                const shortedTree = shortTree(builtTree);
                const actualFileList = listFiles('', shortedTree);

                for (let folder of shortedTree.folders) {
                    Arb.visitTree(folder, (node, nest) => {
                        nest();

                        expect([node.folders.length, node.files.length]).to.not.deep.equal([1, 0]);
                    });
                }

                expect(actualFileList).to.have.members(inputFileList);
            }
        ));
    });
});
