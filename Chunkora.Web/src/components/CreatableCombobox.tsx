import { useState } from "react"
import {
    Combobox,
    ComboboxChip,
    ComboboxChips,
    ComboboxChipsInput,
    ComboboxContent,
    ComboboxEmpty,
    ComboboxItem,
    ComboboxList,
    ComboboxValue,
} from "@/components/ui/combobox"

interface CreatableComboboxProps {
    value: string[]
    onChange: (value: string[]) => void
    options: string[]
    placeholder?: string
}

export function CreatableCombobox({value, onChange, options, placeholder = "Search or create..."}: CreatableComboboxProps) {
    const [input, setInput] = useState("")
    const [createdOptions, setCreatedOptions] = useState<string[]>([])

    const knownOptions = [...new Set([...options, ...createdOptions, ...value])]
    const candidate = input.trim()
    const canCreate = candidate.length > 0 && !knownOptions.some(item => item.toLowerCase() === candidate.toLowerCase())
    const items = canCreate ? [...knownOptions, candidate] : knownOptions

    const handleValueChange = (next: string[]) => {
        const newOptions = next.filter(item => !knownOptions.includes(item))
        if (newOptions.length > 0) {
            setCreatedOptions(prev => [...new Set([...prev, ...newOptions])])
        }
        onChange(next)
    }

    return (
        <Combobox
            multiple
            items={items}
            value={value}
            onValueChange={handleValueChange}
            inputValue={input}
            onInputValueChange={setInput}
            autoHighlight
        >
            <ComboboxChips aria-label="Selected values">
                <ComboboxValue>
                    {
                        value.map(item => (
                            <ComboboxChip key={item}>
                                {item}
                            </ComboboxChip>
                        ))
                    }
                </ComboboxValue>

                <ComboboxChipsInput placeholder={placeholder} aria-label={placeholder} />
            </ComboboxChips>

            <ComboboxContent>
                <ComboboxEmpty>No options found</ComboboxEmpty>

                <ComboboxList>
                    {
                        item => (
                            <ComboboxItem key={item} value={item}>
                                {canCreate && item === candidate ? `* Create "${item}" ...` : item}
                            </ComboboxItem>
                        )
                    }
                </ComboboxList>
            </ComboboxContent>
        </Combobox>
    )
}
