import { ErrorMessage, Form as FormikForm, Formik } from "formik";
import { observer } from "mobx-react-lite";
import { Button, Form, Header, Label } from "semantic-ui-react";
import { useStore } from "../../app/stores/store";
import { Link } from "react-router-dom";

export default observer(function LoginForm() {
    const { userStore } = useStore();

    return (
        <Formik
            initialValues={{ email: '', password: '', error: null }}
            onSubmit={(values, { setErrors }) => userStore.login(values).catch(error =>
                setErrors({ error: 'Invalid email or password' }))}
        >
            {({ handleSubmit, isSubmitting, errors, handleChange, values }) => (
                <FormikForm className='ui form' onSubmit={handleSubmit} autoComplete='off'>
                    <Header as='h2' content='Login to Invoices' color='teal' textAlign='center' />
                    <Form.Input
                        name='email'
                        placeholder='Email'
                        value={values.email}
                        onChange={handleChange}
                    />
                    <Form.Input
                        name='password'
                        placeholder='Password'
                        type='password'
                        value={values.password}
                        onChange={handleChange}
                    />
                    <ErrorMessage
                        name='error' render={() =>
                            <Label style={{ marginBottom: 10 }} basic color='red' content={errors.error} />}
                    />
                    <Button loading={isSubmitting} positive content='Login' type='submit' fluid />

                    <div style={{ textAlign: 'center', marginTop: 15 }}>
                        <Link to='/forgot-password'>Forgot password?</Link>
                    </div>
                </FormikForm>
            )}
        </Formik>
    )
})
