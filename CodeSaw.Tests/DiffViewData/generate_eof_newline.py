import os.path

def generate_cases():
    for operation in ['None', 'Insert', 'Delete', 'Modify']:
        for previous_has_new_line in [True, False]:
            for current_has_new_line in [True, False]:
                yield (operation, previous_has_new_line, current_has_new_line)

def write_case(case_name, file_name, lines):
    directory_name = os.path.join(os.path.dirname(__file__), case_name)

    if not os.path.exists(directory_name):
        os.mkdir(directory_name)

    with open(os.path.join(directory_name, file_name), 'w') as f:
        f.write('\n'.join(lines))

def main():
    base_text = ['Line {:02}'.format(i) for i in range(1, 21)]

    tf_map = {
        True: 'Y',
        False: 'N'
    }

    for (operation, previous_has_new_line, current_has_new_line) in generate_cases():
        previous = list(base_text)
        current = list(base_text)

        if operation == 'Insert':
            current.append('Inserted line')
        elif operation == 'Delete':
            del current[-1]
        elif operation == 'Modify':
            current[-1] = 'Changed line'

        if previous_has_new_line:
            previous.append('')

        if current_has_new_line:
            current.append('')

        case_name = 'EofNewLine_{}{}_{}'.format(tf_map[previous_has_new_line], tf_map[current_has_new_line], operation)

        write_case(case_name, 'previous.txt', previous)
        write_case(case_name, 'current.txt', current)
        write_case(case_name, 'patches.txt', [])
        write_case(case_name, 'patches_margin.txt', [])
        

main()